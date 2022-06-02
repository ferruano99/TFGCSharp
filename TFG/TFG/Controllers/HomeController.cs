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
/**
 * https://www.renfe.com/es/es/cercanias/cercanias-madrid/lineas
 * DECLARE @JSON nvarchar(max)
 
-- load the geojson into the variable
SELECT @JSON = BulkColumn
FROM OPENROWSET (BULK 'F:\uni\TFG\CSharpTFG\TFG\TFG\Content\Data\cercanias\cercanias_estaciones.json', SINGLE_CLOB) as Import
 
-- use OPENJSON to split the different JSON nodes into separate columns
SELECT
	*
FROM
OPENJSON(@JSON, '$')
	WITH (
		CODIGOESTACION int '$.CODIGOESTACION',
		DENOMINACION nvarchar(500) '$.DENOMINACION',
		lon float '$.coordinates[0]',
		lat float '$.coordinates[1]'
	)

 
 
insert into transbordos_ferroviarios (CEMetro, CECercanias, CELigero)
values
(261,18,NULL), --chamartin
(11,122,NULL), --gran via-sol
(12,122,NULL), --sol-sol
(16,11,NULL), --atocha
(25,87,NULL), --sierra de guadalupe-vallecas
(273,92,NULL), --villaverde alto
(46,28,NULL), --acacias embajadores
(93,57,NULL), --piramides
(100,7,NULL), --aluche
(104,40,NULL), --laguna
(111,46,NULL),--méndez álvaro
(120,51,NULL), --nuevos ministerios
(127,61,NULL), --príncipe pío
(288,22,NULL), --coslada central
(154,58,NULL), --pitis
(285,142,NULL), --t4
(345,145,NULL), --paco de lucía
(182,90,NULL), --puerta de arganda-vicálvaro
(261,18,NULL), --chamartin
(203,24,NULL), --4 vientos
(211,5,NULL), --alcorcón central
(214,48,NULL), --móstoles central
(221,32,NULL), --fuenla
(226,34,NULL), --getafe central
(228,103,NULL), --el casar
(235,41,NULL), --lega centtral


(202,NULL,10), --colonia jardin (ligero, metro)
(NULL,133,2), --fuente la mora (ligero, cercanias)
(276,NULL,9), --tablas (ligero, metro)
(NULL,9,23), --aravaca (ligero cercanias)
(NULL,53,53) --parla (ligero, cercanias)**/
namespace TFG.Controllers
{

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string pedestrianPath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-pedestrian.routerdb");
            string carPath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-car.routerdb");
            string busPath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-bus.routerdb");
            string bicyclePath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-bicycle.routerdb");
            if (!System.IO.File.Exists(pedestrianPath))
            {
                SerializeMap(Vehicle.Pedestrian);
            }
            if (!System.IO.File.Exists(carPath))
            {
                SerializeMap(Vehicle.Car);
            }
            if (!System.IO.File.Exists(busPath))
            {
                SerializeMap(Vehicle.Bus);
            }
            if (!System.IO.File.Exists(bicyclePath))
            {
                SerializeMap(Vehicle.Bicycle);
            }
            return View();
        }


        public List<NodeTransport> GetNodes(string[] name)
        {
            List<NodeTransport> nodes = new List<NodeTransport>();
            var denominacion = name[1]; //problemas con linq
            using (var db = new TransportePublicoEntities())
            {
                switch (name[0])
                {
                    case "CERCANIAS":
                        nodes = (from ce in db.cercanias_estacion
                                 join ct in db.cercanias_orden_linea on ce.CODIGOESTACION equals ct.CODIGOESTACION
                                 where denominacion == ct.DENOMINACION
                                 select new NodeTransport
                                 {
                                     id = ct.OBJECTID,
                                     codigoCTM = ct.CODIGOESTACION,
                                     linea = ct.NUMEROLINEAUSUARIO,
                                     ordenLinea = ct.NUMEROORDEN,
                                     denominacion = ct.DENOMINACION,
                                     lat = ce.lat,
                                     lon = ce.lon,
                                     type = Models.Type.CERCANIAS
                                 }).ToList();
                        break;
                    case "METRO":
                        nodes = (from me in db.metro_estacion
                                 join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                 where denominacion == me.DENOMINACION
                                 select new NodeTransport
                                 {
                                     id = me.CODIGOESTACION,
                                     codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                     linea = mol.NUMEROLINEAUSUARIO,
                                     ordenLinea = mol.NUMEROORDEN,
                                     denominacion = me.DENOMINACION,
                                     lat = me.lat,
                                     lon = me.lon,
                                     type = Models.Type.METRO
                                 }).ToList();
                        break;
                    case "LIGERO":
                        nodes = (from le in db.ligero_estacion
                                 join lol in db.ligero_orden_linea on le.CODIGOESTACION equals lol.CODIGOESTACION
                                 where denominacion == le.DENOMINACION
                                 select new NodeTransport
                                 {
                                     id = le.CODIGOESTACION,
                                     codigoCTM = le.CODIGOCTMESTACIONREDMETRO,
                                     ordenLinea = lol.NUMEROORDEN,
                                     linea = lol.NUMEROLINEAUSUARIO,
                                     lat = le.lat,
                                     lon = le.lon,
                                     type = Models.Type.LIGERO
                                 }).ToList();
                        break;
                }
            }
            return nodes;
        }

        public ActionResult FindPath(string Start, string End)
        {
            var start = Start.Split(',');
            var end = End.Split(',');
            using (var db = new TransportePublicoEntities())
            {
                List<NodeTransport> startNodes = GetNodes(start);
                List<NodeTransport> endNodes = GetNodes(end);
                MetroPath(startNodes, endNodes);
                NodeTransport[] waypoints = { startNodes[0], endNodes[0] };

                //GetItinero(startNodes[0], endNodes[0], Vehicle.Car);
                return Json(new
                {
                    Path = Session["Path"] as List<NodeTransport>,
                    CarShape = Session["CarShape"],
                    Waypoints = waypoints
                });
            }
        }



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
            var type = node.type;
            List<NodeTransport> nodelist = new List<NodeTransport>();
            using (var db = new TransportePublicoEntities())
            {
                List<NodeTransport> prevAndNextStations = new List<NodeTransport>();
                List<NodeTransport> transferNodes = new List<NodeTransport>();
                List<TransferCodes> otherStationCodes = new List<TransferCodes>();
                List<NodeTransport> otherTransportTransfers = new List<NodeTransport>();
                switch (type)
                {
                    case Models.Type.METRO:
                        //metemos estaciones contiguas
                        prevAndNextStations = (from me in db.metro_estacion
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
                                                   lon = me.lon,
                                                   type = Models.Type.METRO
                                               }).ToList();
                        nodelist.AddRange(prevAndNextStations);


                        //ahora metemos los transbordos. Primero cogemos el transbordo de metro y luego con el resto de estaciones ferroviarias
                        transferNodes = (from me in db.metro_estacion
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
                                             lon = me.lon,
                                             type = Models.Type.METRO
                                         }).ToList();
                        nodelist.AddRange(transferNodes);

                        //de resto de estaciones
                        otherStationCodes = (from tf in db.transbordos_ferroviarios
                                             join me in db.metro_estacion on tf.CEMetro equals me.CODIGOESTACION
                                             join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION into joined
                                             from j in joined.DefaultIfEmpty()
                                             where me.CODIGOCTMESTACIONREDMETRO == node.codigoCTM
                                             select new TransferCodes
                                             {
                                                 id = tf.ID,
                                                 CECercanias = tf.CECercanias,
                                                 CELigero = tf.CELigero
                                             }).Distinct().ToList();

                        foreach (var codeStation in otherStationCodes)
                        {
                            if (codeStation.CECercanias != null)
                            {
                                otherTransportTransfers.AddRange((from ct in db.cercanias_orden_linea
                                                                  join ce in db.cercanias_estacion on ct.CODIGOESTACION equals ce.CODIGOESTACION
                                                                  where codeStation.CECercanias == ct.CODIGOESTACION
                                                                  select new NodeTransport
                                                                  {
                                                                      id = ct.OBJECTID,
                                                                      codigoCTM = ce.CODIGOESTACION,
                                                                      linea = ct.NUMEROLINEAUSUARIO,
                                                                      ordenLinea = ct.NUMEROORDEN,
                                                                      denominacion = ct.DENOMINACION,
                                                                      lat = ce.lat,
                                                                      lon = ce.lon,
                                                                      type = Models.Type.CERCANIAS
                                                                  }).ToList());
                            }

                            if (codeStation.CELigero != null)
                            {
                                otherTransportTransfers.AddRange((from me in db.ligero_estacion
                                                                  join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                                                  where codeStation.CELigero == me.CODIGOCTMESTACIONREDMETRO
                                                                  select new NodeTransport
                                                                  {
                                                                      id = me.CODIGOESTACION,
                                                                      codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                                                      linea = mol.NUMEROLINEAUSUARIO,
                                                                      ordenLinea = mol.NUMEROORDEN,
                                                                      denominacion = me.DENOMINACION,
                                                                      lat = me.lat,
                                                                      lon = me.lon,
                                                                      type = Models.Type.LIGERO
                                                                  }).ToList());
                            }

                        }
                        nodelist.AddRange(otherTransportTransfers);

                        break;
                    case Models.Type.CERCANIAS:
                        prevAndNextStations = (from ce in db.cercanias_estacion
                                               join ct in db.cercanias_orden_linea on ce.CODIGOESTACION equals ct.CODIGOESTACION
                                               where node.linea == ct.NUMEROLINEAUSUARIO && (ct.NUMEROORDEN - 1 == node.ordenLinea || ct.NUMEROORDEN + 1 == node.ordenLinea)
                                               select new NodeTransport
                                               {
                                                   id = ct.OBJECTID,
                                                   codigoCTM = ct.CODIGOESTACION,
                                                   denominacion = ct.DENOMINACION,
                                                   linea = ct.NUMEROLINEAUSUARIO,
                                                   ordenLinea = ct.NUMEROORDEN,
                                                   lat = ce.lat,
                                                   lon = ce.lon,
                                                   type = Models.Type.CERCANIAS
                                               }).ToList();
                        nodelist.AddRange(prevAndNextStations);

                        //ahora metemos los transbordos. Primero cogemos el transbordo de metro y luego con el resto de estaciones ferroviarias
                        transferNodes = (from ce in db.cercanias_estacion
                                         join ct in db.cercanias_orden_linea on ce.CODIGOESTACION equals ct.CODIGOESTACION
                                         where node.codigoCTM == ct.CODIGOESTACION && node.id != ct.OBJECTID
                                         select new NodeTransport
                                         {
                                             id = ct.OBJECTID,
                                             codigoCTM = ct.CODIGOESTACION,
                                             denominacion = ct.DENOMINACION,
                                             linea = ct.NUMEROLINEAUSUARIO,
                                             ordenLinea = ct.NUMEROORDEN,
                                             lat = ce.lat,
                                             lon = ce.lon,
                                             type = Models.Type.CERCANIAS
                                         }).ToList();
                        nodelist.AddRange(transferNodes);

                        //de resto de estaciones
                        otherStationCodes = (from tf in db.transbordos_ferroviarios
                                             join ce in db.cercanias_estacion on tf.CECercanias equals ce.CODIGOESTACION
                                             join ct in db.cercanias_orden_linea on ce.CODIGOESTACION equals ct.CODIGOESTACION into joined //left join
                                             from j in joined.DefaultIfEmpty()
                                             where ce.CODIGOESTACION == node.codigoCTM
                                             select new TransferCodes
                                             {
                                                 id = tf.ID,
                                                 CEMetro = tf.CEMetro,
                                                 CELigero = tf.CELigero
                                             }).Distinct().ToList();

                        foreach (var codeStation in otherStationCodes)
                        {
                            if (codeStation.CEMetro != null)
                            {
                                otherTransportTransfers.AddRange((from me in db.metro_estacion
                                                                  join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                                                  where codeStation.CEMetro == me.CODIGOCTMESTACIONREDMETRO
                                                                  select new NodeTransport
                                                                  {
                                                                      id = me.CODIGOESTACION,
                                                                      codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                                                      linea = mol.NUMEROLINEAUSUARIO,
                                                                      ordenLinea = mol.NUMEROORDEN,
                                                                      denominacion = me.DENOMINACION,
                                                                      lat = me.lat,
                                                                      lon = me.lon,
                                                                      type = Models.Type.METRO
                                                                  }).ToList());
                            }

                            if (codeStation.CELigero != null)
                            {
                                otherTransportTransfers.AddRange((from me in db.ligero_estacion
                                                                  join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                                                  where codeStation.CELigero == me.CODIGOCTMESTACIONREDMETRO
                                                                  select new NodeTransport
                                                                  {
                                                                      id = me.CODIGOESTACION,
                                                                      codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                                                      linea = mol.NUMEROLINEAUSUARIO,
                                                                      ordenLinea = mol.NUMEROORDEN,
                                                                      denominacion = me.DENOMINACION,
                                                                      lat = me.lat,
                                                                      lon = me.lon,
                                                                      type = Models.Type.LIGERO
                                                                  }).ToList());
                            }
                        }
                        nodelist.AddRange(otherTransportTransfers);

                        break;
                    case Models.Type.LIGERO:
                        //metemos estaciones contiguas
                        prevAndNextStations = (from me in db.ligero_estacion
                                               join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                               where node.linea == mol.NUMEROLINEAUSUARIO && (mol.NUMEROORDEN - 1 == node.ordenLinea || mol.NUMEROORDEN + 1 == node.ordenLinea)
                                               select new NodeTransport
                                               {
                                                   id = me.CODIGOESTACION,
                                                   codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                                   denominacion = me.DENOMINACION,
                                                   linea = mol.NUMEROLINEAUSUARIO,
                                                   ordenLinea = mol.NUMEROORDEN,
                                                   lat = me.lat,
                                                   lon = me.lon,
                                                   type = Models.Type.LIGERO
                                               }).ToList();
                        nodelist.AddRange(prevAndNextStations);


                        //ahora metemos los transbordos. Primero cogemos el transbordo de metro y luego con el resto de estaciones ferroviarias
                        transferNodes = (from me in db.ligero_estacion
                                         join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                         where node.codigoCTM == me.CODIGOCTMESTACIONREDMETRO && node.id != me.CODIGOESTACION
                                         select new NodeTransport
                                         {
                                             id = me.CODIGOESTACION,
                                             codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                             denominacion = me.DENOMINACION,
                                             linea = mol.NUMEROLINEAUSUARIO,
                                             ordenLinea = mol.NUMEROORDEN,
                                             lat = me.lat,
                                             lon = me.lon,
                                             type = Models.Type.LIGERO
                                         }).ToList();
                        nodelist.AddRange(transferNodes);

                        //de resto de estaciones
                        otherStationCodes = (from tf in db.transbordos_ferroviarios
                                             join me in db.ligero_estacion on tf.CELigero equals me.CODIGOESTACION
                                             join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION into joined
                                             from j in joined.DefaultIfEmpty()
                                             where me.CODIGOCTMESTACIONREDMETRO == node.codigoCTM
                                             select new TransferCodes
                                             {
                                                 id = tf.ID,
                                                 CECercanias = tf.CECercanias,
                                                 CEMetro = tf.CEMetro
                                             }).Distinct().ToList();

                        foreach (var codeStation in otherStationCodes)
                        {
                            if (codeStation.CECercanias != null)
                            {
                                otherTransportTransfers.AddRange((from ct in db.cercanias_orden_linea
                                                                  join ce in db.cercanias_estacion on ct.CODIGOESTACION equals ce.CODIGOESTACION
                                                                  where codeStation.CECercanias == ct.CODIGOESTACION
                                                                  select new NodeTransport
                                                                  {
                                                                      id = ct.OBJECTID,
                                                                      codigoCTM = ce.CODIGOESTACION,
                                                                      linea = ct.NUMEROLINEAUSUARIO,
                                                                      ordenLinea = ct.NUMEROORDEN,
                                                                      denominacion = ct.DENOMINACION,
                                                                      lat = ce.lat,
                                                                      lon = ce.lon,
                                                                      type = Models.Type.CERCANIAS
                                                                  }).ToList());
                            }
                            if (codeStation.CEMetro != null)
                            {
                                otherTransportTransfers.AddRange((from me in db.metro_estacion
                                                                  join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                                                  where codeStation.CEMetro == me.CODIGOCTMESTACIONREDMETRO
                                                                  select new NodeTransport
                                                                  {
                                                                      id = me.CODIGOESTACION,
                                                                      codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                                                      linea = mol.NUMEROLINEAUSUARIO,
                                                                      ordenLinea = mol.NUMEROORDEN,
                                                                      denominacion = me.DENOMINACION,
                                                                      lat = me.lat,
                                                                      lon = me.lon,
                                                                      type = Models.Type.METRO
                                                                  }).ToList());
                            }
                        }
                        nodelist.AddRange(otherTransportTransfers);

                        break;
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
            }
            else if (vehicle.Equals(Vehicle.Bus))
            {

            }

            using (var stream = new FileInfo(path).OpenRead())
            {
                routerDb = RouterDb.Deserialize(stream, RouterDbProfile.NoCache);
                var router = new Router(routerDb);
                var profile = vehicle.Fastest();
                //https://www.google.com/search?q=build+x64+visual+studio&sxsrf=APq-WBuhyq-UCQbZrwKAUD16nxI7zj9F2A%3A1650654811681&ei=W_5iYtigKZe7lwTc06OABA&ved=0ahUKEwiYusmtsKj3AhWX3YUKHdzpCEAQ4dUDCA8&uact=5&oq=build+x64+visual+studio&gs_lcp=Cgxnd3Mtd2l6LXNlcnAQAzIHCCMQsAMQJzIHCCMQsAMQJzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwA0oECEEYAEoECEYYAFAAWABglhNoAnAAeACAAQCIAQCSAQCYAQDIAQrAAQE&sclient=gws-wiz-serp
                //https://stackoverflow.com/questions/18892159/why-cant-i-set-asp-net-mvc-4-project-to-be-x64

                //Error dimensiones matriz //cambiar compilador a x64
                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/5850560d-a4a3-4cf5-be9e-b71026d1e175/systemoutofmemoryexception-array-dimensions-exceeded-supported-range-on-dataset?forum=vbgeneral
                var start = router.Resolve(profile, Convert.ToSingle(orig.lat), Convert.ToSingle(orig.lon));

                var end = router.Resolve(profile, Convert.ToSingle(dest.lat), Convert.ToSingle(dest.lon));

                var route = router.Calculate(profile, start, end);

                if (vehicle.Equals(Vehicle.Pedestrian))
                {
                    Session["PedestrianShape"] = route.Shape;
                    Session["PedestrianDistance"] = route.TotalDistance;
                    Session["PedestrianTime"] = route.TotalTime;
                }
                else if (vehicle.Equals(Vehicle.Car))
                {
                    Session["CarShape"] = route.Shape;
                    Session["CarTime"] = route.TotalTime;
                }

            }
        }

        public void SerializeMap(Itinero.Profiles.Vehicle vehicle)
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
            else if (vehicle.Equals(Vehicle.Bus))
            {
                vehiclePath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-bus.routerdb");
            }
            else if (vehicle.Equals(Vehicle.Bicycle))
            {
                vehiclePath = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-bicycle.routerdb");
            }
            using (var stream = new FileInfo(vehiclePath).Open(FileMode.Create))
            {
                routerDb.Serialize(stream);
            }
        }



        public ActionResult GetAllNameStations()
        {
            List<string> stations = new List<string>();
            //TODO: falta por añadir más estaciones
            using (var db = new TransportePublicoEntities())
            {
                List<string> metro = (from m in db.metro_estacion

                                      select "METRO - " + m.DENOMINACION).Distinct().ToList();

                stations.AddRange(metro);

                List<string> cercanias = (from c in db.cercanias_orden_linea
                                          select "CERCANIAS - " + c.DENOMINACION).Distinct().ToList();

                List<string> ligero = (from l in db.ligero_estacion
                                       select "M. LIGERO - " + l.DENOMINACION).Distinct().ToList();

                stations.AddRange(cercanias);
                return Json(new
                {
                    Stations = stations
                }, JsonRequestBehavior.AllowGet); ;
            }
        }
    }
}