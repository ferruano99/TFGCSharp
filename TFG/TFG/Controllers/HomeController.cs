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
            using (var db = new TransportePublicoEntities())
            {
                var start = form["Start"].ToString();
                var end = form["End"].ToString();

                List<NodeTransport> startNodes = new List<NodeTransport>();
                List<NodeTransport> endNodes = new List<NodeTransport>();
                startNodes = (from me in db.metro_estacion
                              join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                              where me.DENOMINACION == start.ToUpper()
                              select new NodeTransport
                              {
                                  id = me.CODIGOESTACION,
                                  codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                  linea = mol.NUMEROLINEAUSUARIO,
                                  ordenLinea = mol.NUMEROORDEN,
                                  denominacion = me.DENOMINACION,
                                  lat = me.lat,
                                  lon = me.lon,
                              }).ToList();
                endNodes = (from me in db.metro_estacion
                              join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                              where me.DENOMINACION == end.ToUpper()
                              select new NodeTransport
                              {
                                  id = me.CODIGOESTACION,
                                  codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                  linea = mol.NUMEROLINEAUSUARIO,
                                  ordenLinea = mol.NUMEROORDEN,
                                  denominacion = me.DENOMINACION,
                                  lat = me.lat,
                                  lon = me.lon,
                              }).ToList();
                MetroPath(startNodes, endNodes);

                GetItinero(startNodes[0], endNodes[0], Vehicle.Car);
                return Json(new
                {
                    Path = Session["Path"] as List<NodeTransport>,
                    CarShape = Session["CarShape"] 
                });
            }
        }

        //public ActionResult FindPath(FormCollection form)
        //{
        //    double latOrig = double.Parse(form["latOrig"], CultureInfo.InvariantCulture);
        //    double lonOrig = double.Parse(form["lonOrig"], CultureInfo.InvariantCulture);
        //    double latDest = double.Parse(form["latDest"], CultureInfo.InvariantCulture);
        //    double lonDest = double.Parse(form["lonDest"], CultureInfo.InvariantCulture);

        //    NodeTransport orig = new NodeTransport { lat = latOrig, lon = lonOrig, denominacion = "ORIGEN" };
        //    NodeTransport dest = new NodeTransport { lat = latDest, lon = lonDest, denominacion = "DESTINO" };

        //    MetroPath(orig, dest);



        //    return Json(new
        //    {
        //        Path = Session["Path"] as List<NodeTransport>
        //    });
        //}


        void MetroPath(List<NodeTransport> origNearList, List<NodeTransport> destNearList)
        {
            List<NodeTransport> closedSet = new List<NodeTransport>();

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
                Session["Path"] = closedSet;
            }
            else //nodos q no tienen línea directa
            {
                //montar A*? o qué
                Astar(origNearList[0], destNearList);
            }

            //Session["Path"] = closedSet;
        }

        List<string> GetMetroLines(NodeTransport node)
        {
            using (var db = new TransportePublicoEntities())
            {

                return (from me in db.metro_estacion
                        join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                        where me.CODIGOCTMESTACIONREDMETRO == node.codigoCTM && node.linea != mol.NUMEROLINEAUSUARIO
                        select mol.NUMEROLINEAUSUARIO
                        ).ToList();
            }
        }


        List<NodeTransport> GetTransferStations(string metroLine) //estaciones de una línea con transbordos
        {
            var nodeTransportStations = new List<NodeTransport>();
            using (var db = new TransportePublicoEntities())
            {
                var transferStations = db.sp_get_transfers_from_line(metroLine);
                foreach (var station in transferStations)
                {
                    nodeTransportStations.Add(new NodeTransport
                    {
                        id = station.CODIGOESTACION,
                        denominacion = station.DENOMINACION,
                        ordenLinea = station.NUMEROORDEN,
                        linea = station.NUMEROLINEAUSUARIO,
                        lat = station.lat,
                        lon = station.lon,
                    });
                }
            }
            return nodeTransportStations;
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
                            candidates = GetBTCandidatesList(origNodeList[step], destNodeList[i]);
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


        List<NodeTransport> GetNearestMetroStations(NodeTransport node)
        {
            List<NodeTransport> nodesList = new List<NodeTransport>();
            double radiusDistance = 0.0005;
            while (nodesList.Count <= 0 && radiusDistance < 0.05) //Mientras no tengas nodos o el radio sea enorme
            {

                using (var db = new TransportePublicoEntities())
                {
                    var nearestNodeList = (from me in db.metro_estacion
                                           join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                           where me.lat + radiusDistance > node.lat && me.lat - radiusDistance < node.lat &&
                                               me.lon + radiusDistance > node.lon && me.lon - radiusDistance < node.lon
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
                radiusDistance += 0.0005;
            }
            return nodesList;
        }



        private void Astar(NodeTransport orig, List<NodeTransport> destList) //https://www.youtube.com/watch?v=mZfyt03LDH4&list=PLFt_AvWsXl0cq5Umv3pMC9SPnKjfp9eGW&index=3&ab_channel=SebastianLague
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

                if (destList.Contains(currentNode))
                {
                    RetracePath(orig, currentNode); //Hay q hacer traceback y recoger el path, ya q hay nodos q no necesitamos
                    return;
                }
                var neighbours = GetNeighbours(currentNode);
                foreach (NodeTransport neighbour in neighbours)
                {
                    //metemos los nodos en la lista abierta que no estén en la lista cerrada
                    if (closedSet.Contains(neighbour)) //No sé si va a funcionar
                    {
                        continue;
                    }

                    double newMovementCostToNeighbour = currentNode.gCost + 1/*GetEuclideanDistance(currentNode, neighbour)*/;
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        if (currentNode.linea != neighbour.linea)
                            neighbour.hCost += 5;
                        //neighbour.hCost = GetEuclideanDistance(neighbour, dest) / 100;
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
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
            path.Add(currentNode);
            path.Reverse();
            Session["Path"] = path;
        }


        private List<NodeTransport> GetNeighbours(NodeTransport node)
        {
            List<NodeTransport> nodelist = new List<NodeTransport>();
            using (var db = new TransportePublicoEntities())
            {
                if (node.denominacion == "ORIGEN")
                {
                    nodelist = GetNearestMetroStations(node);
                }
                else
                {
                    //recogemos los transbordos
                    var transferNodes = (from me in db.metro_estacion
                                         join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                         where node.codigoCTM == me.CODIGOCTMESTACIONREDMETRO && node.id != me.CODIGOESTACION
                                         select new NodeTransport
                                         {
                                             id = me.CODIGOESTACION,
                                             codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                             denominacion = me.DENOMINACION,
                                             linea = mol.NUMEROLINEAUSUARIO,
                                             ordenLinea = mol.NUMEROORDEN,
                                             lat = me.lat,
                                             lon = me.lon
                                         }).ToList();
                    nodelist.AddRange(transferNodes);
                    //recogemos línea previa o siguiente
                    var prevAndNextStations = (from me in db.metro_estacion
                                               join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                               where node.linea == mol.NUMEROLINEAUSUARIO && (mol.NUMEROORDEN - 1 == node.ordenLinea || mol.NUMEROORDEN + 1 == node.ordenLinea)
                                               select new NodeTransport
                                               {
                                                   id = me.CODIGOESTACION,
                                                   codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                                   denominacion = me.DENOMINACION,
                                                   linea = mol.NUMEROLINEAUSUARIO,
                                                   ordenLinea = mol.NUMEROORDEN,
                                                   lat = me.lat,
                                                   lon = me.lon
                                               }).ToList();
                    nodelist.AddRange(prevAndNextStations);
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
        public void GetItinero(NodeTransport orig, NodeTransport dest, Itinero.Profiles.Vehicle vehicle) //https://blog.vincentcos.tel/itinero/
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
            } else if (vehicle.Equals(Vehicle.Bus))
            {

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

                if (vehicle.Equals(Vehicle.Pedestrian))
                {
                    Session["PedestrianShape"] = route.Shape;
                    Session["PedestrianDistance"] = route.TotalDistance;
                    Session["PedestrianTime"] = route.TotalTime;
                } else if (vehicle.Equals(Vehicle.Car))
                {
                    Session["CarShape"] = route.Shape;
                    Session["CarTime"] = route.TotalTime;
                }

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



        public ActionResult GetAllNameStations()
        {
            //TODO: falta por añadir más estaciones
            using (var db = new TransportePublicoEntities())
            {
                var stations = (from m in db.metro_estacion

                                select m.DENOMINACION.Substring(0, 1) + m.DENOMINACION.Substring(1).ToLower()
                                ).Distinct().ToList();

                return Json(new
                {
                    Stations = stations
                }, JsonRequestBehavior.AllowGet); ;
            }
        }
    }
}