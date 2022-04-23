using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Itinero.Transit; //ver si funciona
using System;
using System.Collections.Generic;
using System.Globalization;
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
                SerializeMap(Vehicle.Pedestrian); //1 para peatón
            }
            if (!System.IO.File.Exists(carPath))
            {
                SerializeMap(Vehicle.Pedestrian); //2 para coche
            }
            return View();
        }

        public ActionResult FindPath(FormCollection form)
        {
            var valor = form["latOrig"].ToString();
            var valor2 = form["latOrig"];
            double latOrig = double.Parse(form["latOrig"], CultureInfo.InvariantCulture);
            double lonOrig = double.Parse(form["lonOrig"], CultureInfo.InvariantCulture);
            double latDest = double.Parse(form["latDest"], CultureInfo.InvariantCulture);
            double lonDest = double.Parse(form["lonDest"], CultureInfo.InvariantCulture);

            NodeTransport orig = new NodeTransport { lat = latOrig, lon = lonOrig };
            NodeTransport dest = new NodeTransport { lat = latDest, lon = lonDest };

            //var path = Astar(orig, dest);



            return Json(new
            {
                Hola = GetDistanceItinero(orig, dest, Vehicle.Car)
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
            string path = null;
            if (vehicle.Equals(Vehicle.Car))
            {
                path = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-car.routerdb");
            }
            else if (vehicle.Equals(Vehicle.Pedestrian))
            {
                path = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-pedestrian.routerdb");

            }
            using (var stream = new FileInfo(path).OpenRead())
            {
                routerDb = RouterDb.Deserialize(stream, RouterDbProfile.NoCache);
                var router = new Router(routerDb);
                var profile = vehicle.Fastest();
                //https://www.google.com/search?q=build+x64+visual+studio&sxsrf=APq-WBuhyq-UCQbZrwKAUD16nxI7zj9F2A%3A1650654811681&ei=W_5iYtigKZe7lwTc06OABA&ved=0ahUKEwiYusmtsKj3AhWX3YUKHdzpCEAQ4dUDCA8&uact=5&oq=build+x64+visual+studio&gs_lcp=Cgxnd3Mtd2l6LXNlcnAQAzIHCCMQsAMQJzIHCCMQsAMQJzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwA0oECEEYAEoECEYYAFAAWABglhNoAnAAeACAAQCIAQCSAQCYAQDIAQrAAQE&sclient=gws-wiz-serp
                //https://stackoverflow.com/questions/18892159/why-cant-i-set-asp-net-mvc-4-project-to-be-x64

                //Error dimensiones matriz
                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/5850560d-a4a3-4cf5-be9e-b71026d1e175/systemoutofmemoryexception-array-dimensions-exceeded-supported-range-on-dataset?forum=vbgeneral
                var start = router.Resolve(profile, Convert.ToSingle(orig.lat), Convert.ToSingle(orig.lon)); //cambiar compilador a x64

                var end = router.Resolve(profile, Convert.ToSingle(dest.lat), Convert.ToSingle(dest.lon));

                var route = router.Calculate(profile, start, end);

                return route.TotalDistance;
            }
        }

        private void SerializeMap(Itinero.Profiles.Vehicle vehicle)
        {
            //Estos métodos se van a ejecutar para serializar y leer más rápido desde itinero.
            var routerDb = new RouterDb();
            string path = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/madrid-latest.osm.pbf");
            using (var stream = new FileInfo(path).OpenRead())
            {
                routerDb.LoadOsmData(stream, vehicle);
            }
            string vehiclePath = "";

            if (vehicle.Equals(Vehicle.Car))
            {
                vehiclePath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-car.routerdb");
            }
            else if (vehicle.Equals(Vehicle.Pedestrian))
            {
                vehiclePath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-pedestrian.routerdb");
            }

            using (var stream = new FileInfo(vehiclePath).Open(FileMode.Create))
            {
                routerDb.Serialize(stream);
            }
        }

    }
}