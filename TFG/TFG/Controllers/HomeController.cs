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

            NodeTransport orig = new NodeTransport { lat = latOrig, lon = lonOrig, denominacion = "ORIGEN" };
            NodeTransport dest = new NodeTransport { lat = latDest, lon = lonDest, denominacion = "DESTINO" };

            MetroPath(orig, dest);



            return Json(new
            {
                Path = Session["Path"] as List<NodeTransport>
            });
        }


        void MetroPath(NodeTransport orig, NodeTransport dest)
        {
            List<NodeTransport> closedSet = new List<NodeTransport>();
            //origen
            var origNearList = GetNearestMetroStations(orig, dest);

            //destino
            var destNearList = GetNearestMetroStations(dest, orig);

            //de momento dejar
            closedSet.Add(orig);
            closedSet.Add(dest);

            if (!origNearList.Contains(dest))
            {
                List<NodeTransport> candidateNodes = new List<NodeTransport>();
                foreach (var origNode in origNearList) //BT?
                {
                    if (destNearList.Any(destNode => origNode.linea == destNode.linea))
                    {
                        candidateNodes.Add(origNode);
                    }
                }
                if (candidateNodes.Count > 0) //Nodos que tienen línea directa
                {
                    closedSet = BTDirectLines(candidateNodes, destNearList, closedSet, 0);
                }
                else
                {

                }
            }
            Session["Path"] = closedSet;
        }

        List<NodeTransport> BTDirectLines(List<NodeTransport> origNodeList, List<NodeTransport> destNodeList, List<NodeTransport> sol, int step)
        {
            for (int i = 0; i < destNodeList.Count; i++)
            {
                if (step < origNodeList.Count) //factible
                {
                    if (destNodeList[i].linea == origNodeList[step].linea) //factible
                    {
                        var candidates = new List<NodeTransport>();
                        using (var db = new TransportePublicoEntities()) //recogemos datos
                        {
                            candidates = GetBTCandidatesList(origNodeList[i], destNodeList[step]);
                            if (sol.Count <= 0)
                            {
                                sol = candidates;
                            }
                            else if (candidates.Count < sol.Count && sol.Count > 0) //optimalidad
                            {
                                sol = candidates;
                            }
                        }
                        BTDirectLines(origNodeList, destNodeList, sol, step + 1); //bt
                    }
                }
            }
            return sol;

        }
        List<NodeTransport> GetBTCandidatesList(NodeTransport nodeOrig, NodeTransport nodeDest)
        {
            using (var db = new TransportePublicoEntities())
            {
                return nodeOrig.ordenLinea > nodeDest.ordenLinea
                    ? (from me in db.metro_estacion
                       join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                       where mol.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                       mol.NUMEROORDEN <= nodeOrig.ordenLinea && mol.NUMEROORDEN >= nodeDest.ordenLinea
                       orderby mol.NUMEROORDEN descending
                       select new NodeTransport
                       {
                           id = me.CODIGOESTACION,
                           linea = mol.NUMEROLINEAUSUARIO,
                           denominacion = me.DENOMINACION,
                           ordenLinea = mol.NUMEROORDEN,
                           lat = me.lat,
                           lon = me.lon
                       }).ToList()
                    : (from me in db.metro_estacion
                       join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                       where mol.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                       mol.NUMEROORDEN >= nodeOrig.ordenLinea && mol.NUMEROORDEN <= nodeDest.ordenLinea
                       orderby mol.NUMEROORDEN
                       select new NodeTransport
                       {
                           id = me.CODIGOESTACION,
                           linea = mol.NUMEROLINEAUSUARIO,
                           denominacion = me.DENOMINACION,
                           ordenLinea = mol.NUMEROORDEN,
                           lat = me.lat,
                           lon = me.lon
                       }).ToList();
            }
        }

        List<NodeTransport> GetNearestMetroStations(NodeTransport orig, NodeTransport dest)
        {
            List<NodeTransport> nodesList = new List<NodeTransport>();
            double radiusDistance = 0.005;
            while (nodesList.Count <= 0 && !nodesList.Contains(dest)) //Mientras no tengas nodos o no tengas el nodo destino
            {
                if (orig.lat + radiusDistance > dest.lat && orig.lat - radiusDistance < dest.lat && orig.lon + radiusDistance > dest.lon && orig.lon - radiusDistance < dest.lon)
                    nodesList.Add(dest); //caso de que estén cerca
                else
                {
                    using (var db = new TransportePublicoEntities())
                    {
                        var nearestNodeList = (from me in db.metro_estacion
                                               join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                               where me.lat + radiusDistance > orig.lat && me.lat - radiusDistance < orig.lat &&
                                                   me.lon + radiusDistance > orig.lon && me.lon - radiusDistance < orig.lon
                                               select new NodeTransport
                                               {
                                                   id = me.CODIGOESTACION,
                                                   linea = mol.NUMEROLINEAUSUARIO,
                                                   ordenLinea = mol.NUMEROORDEN,
                                                   denominacion = me.DENOMINACION,
                                                   lat = me.lat,
                                                   lon = me.lon,
                                                   //parent = orig //necesario?
                                               }).ToList();
                        nodesList = nearestNodeList;
                    }
                    radiusDistance += 0.005;
                }
            }
            return nodesList;
        }







        private void Astar(NodeTransport orig, NodeTransport dest) //https://www.youtube.com/watch?v=mZfyt03LDH4&list=PLFt_AvWsXl0cq5Umv3pMC9SPnKjfp9eGW&index=3&ab_channel=SebastianLague
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
                    RetracePath(orig, dest); //Hay q hacer traceback y recoger el path, ya q hay nodos q no necesitamos
                }

                foreach (NodeTransport neighbour in GetNeighbours(currentNode, dest))
                {
                    //metemos los nodos en la lista abierta que no estén en la lista cerrada
                    if (closedSet.Any(node => node.denominacion == neighbour.denominacion && node.linea == neighbour.linea)) //No sé si va a funcionar
                    {
                        continue;
                    }
                    //Recoger ladistancia (probablemente lo haga con itinero para el primer nodo, que será al q tenga que ir andando. Para el resto, mediante distancia euclídea)

                    double newMovementCostToNeighbour = currentNode.gCost + GetEuclideanDistance(currentNode, neighbour);
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Any(node => node.denominacion == neighbour.denominacion && node.linea == neighbour.linea))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetEuclideanDistance(neighbour, dest);
                        neighbour.parent = currentNode;

                        if (!openSet.Any(node => node.denominacion == neighbour.denominacion && node.linea == neighbour.linea))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }
        }

        private void RetracePath(NodeTransport start, NodeTransport end)
        {
            var path = new List<NodeTransport>();
            NodeTransport currentNode = end;
            while (currentNode != start)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }

            path.Reverse();
            Session["Path"] = path;
        }


        private List<NodeTransport> GetNeighbours(NodeTransport node, NodeTransport dest)
        {
            using (var db = new TransportePublicoEntities())
            {
                var nodelist = (from me in db.metro_estacion //Únicamente metro
                                join mt in db.metro_orden_linea on me.CODIGOESTACION equals mt.CODIGOESTACION
                                where (node.lat > me.lat - 0.01 && node.lat < me.lat + 0.01) && (node.lon > me.lon - 0.01 && node.lon < me.lon + 0.01)
                                select new NodeTransport
                                {
                                    id = mt.CODIGOESTACION,
                                    linea = mt.NUMEROLINEAUSUARIO,
                                    denominacion = me.DENOMINACION,
                                    lat = me.lat,
                                    lon = me.lon
                                }).ToList();
                if (node.lat - 0.01 < dest.lat && node.lat + 0.01 > dest.lat && node.lon - 0.01 < dest.lon && node.lon + 0.01 > dest.lon)
                {
                    nodelist.Add(dest);
                }
                return nodelist;
            }
        }

        private double GetEuclideanDistance(NodeTransport orig, NodeTransport dest)
        {
            double lats = Math.Pow((double)orig.lat + (double)dest.lat, 2);
            double lons = Math.Pow((double)orig.lon + (double)dest.lon, 2);
            return Math.Sqrt(lats + lons);
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