using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using TFG.Models;

namespace TFG.Controllers
{

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string pedestrianPath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-pedestrian.routerdb");
            string carPath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-car.routerdb");
            if (!System.IO.File.Exists(pedestrianPath))
            {
                SerializeMap(1); //1 para peatón
            }
            if (!System.IO.File.Exists(carPath))
            {
                SerializeMap(2); //2 para coche
            }
            return View();
        }

        public ActionResult FindPath(FormCollection form)
        {
            double latOrig = double.Parse(form["latOrig"]);
            double lonOrig = double.Parse(form["lonOrig"]);
            double latDest = double.Parse(form["latDest"]);
            double lonDest = double.Parse(form["lonDest"]);

            NodeTransport orig = new NodeTransport { lat = latOrig, lon = lonOrig };
            NodeTransport dest = new NodeTransport { lat = latDest, lon = lonDest };

            //var path = Astar(orig, dest);



            return Json(new
            {
                Hola = GetDistanceItinero(orig, dest, Vehicles.Car)
            });
        }

        private HashSet<NodeTransport> Astar(NodeTransport orig, NodeTransport dest) //https://www.youtube.com/watch?v=mZfyt03LDH4&list=PLFt_AvWsXl0cq5Umv3pMC9SPnKjfp9eGW&index=3&ab_channel=SebastianLague
        {
            List<NodeTransport> openSet = new List<NodeTransport>();
            HashSet<NodeTransport> closedSet = new HashSet<NodeTransport>();

            openSet.Add(orig);
            while (openSet.Count > 0)
            {
                NodeTransport currentNode = openSet[0];
                for (int i = 0; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost) //Condición optimalidad
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode); //Lo metemos en la lista cerrada

                if (currentNode == dest)
                {
                    return closedSet; //Hay q hacer traceback y recoger el path, ya q hay nodos q no necesitamos
                }

                foreach (NodeTransport neighbour in GetNeighbours(currentNode))
                {
                    //metemos los nodos en la lista abierta que no estén en la lista cerrada
                    if (closedSet.Any(node => node.denominacion == neighbour.denominacion && node.linea == neighbour.linea)) //No sé si va a funcionar
                    {
                        continue;
                    }
                    //Recoger ladistancia (probablemente lo haga con itinero para el primer nodo, que será al q tenga que ir andando. Para el resto, mediante distancia euclídea)
                }
            }
            return closedSet;
        }


        private List<NodeTransport> GetNeighbours(NodeTransport node)
        {
            using (var db = new TransportePublicoEntities())
            {
                return (from me in db.metro_estacion //Únicamente metro
                        join mt in db.metro_tramo on me.CODIGOESTACION equals mt.CODIGOESTACION
                        where (node.lat > me.lat - 0.01 && node.lat < me.lat + 0.01) && (node.lon > me.lon - 0.01 && node.lon < me.lon + 0.01)
                        select new NodeTransport
                        {
                            id = mt.CODIGOESTACION,
                            linea = mt.NUMEROLINEAUSUARIO,
                            denominacion = me.DENOMINACION,
                            tipo = 1, //metro
                            lat = me.lat,
                            lon = me.lon
                        }).ToList();

            }
        }

        private float GetDistanceItinero(NodeTransport orig, NodeTransport dest, Itinero.Profiles.Vehicle vehicle) //https://blog.vincentcos.tel/itinero/
        {
            RouterDb routerDb = null;
            if (vehicle.Equals(Vehicle.Car))
            {
                var path = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-car.routerdb");

                using (var stream = new FileInfo(path).OpenRead())
                {
                    routerDb = RouterDb.Deserialize(stream);
                }
            }
            else if (vehicle.Equals(Vehicle.Pedestrian))
            {
                var path = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-pedestrian.routerdb");

                using (var stream = new FileInfo(path).OpenRead())
                {
                    routerDb = RouterDb.Deserialize(stream);
                }
            }
            var router = new Router(routerDb);
            var profile = vehicle.Fastest();

            var start = router.Resolve(profile, Convert.ToSingle(orig.lat), Convert.ToSingle(orig.lon));
            var end = router.Resolve(profile, Convert.ToSingle(dest.lat), Convert.ToSingle(dest.lon));

            var route = router.Calculate(profile, start, end);

            return route.TotalDistance;
        }

        private void SerializeMap(Itinero.Profiles.Vehicle vehicle)
        {
            //Estos métodos se van a ejecutar para serializar y leer más rápido desde itinero.
            var routerDb = new RouterDb();
            string path = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/madrid-latest.osm.pbf");
            using (var stream = new FileInfo(path).OpenRead())
            {
                routerDb.LoadOsmData(stream, Vehicle.Car);
            }
            string vehiclePath = "";

            if (vehicle.Equals(Vehicle.Car))
            {
                vehiclePath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-pedestrian.routerdb");
            }
            else if (vehicle.Equals(Vehicle.Pedestrian))
            {
                vehiclePath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-car.routerdb");
            }

            using (var stream = new FileInfo(vehiclePath).Open(FileMode.Create))
            {
                routerDb.Serialize(stream);
            }
        }

    }
}