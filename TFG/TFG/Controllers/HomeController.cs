using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
            List<TransferCodes> otherStationCodes = new List<TransferCodes>();
            List<NodeTransport> otherTransportTransfers = new List<NodeTransport>();
            string denominacion = "";
            for (int i = 1; i < name.Length; i++)
            {
                denominacion += name[i];
            }
            NodeTransport primerNodo;
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

                        primerNodo = nodes[0];
                        otherStationCodes = (from tf in db.transbordos_ferroviarios
                                             join col in db.cercanias_orden_linea on tf.CECercanias equals col.CODIGOESTACION
                                             join ce in db.cercanias_estacion on col.CODIGOESTACION equals ce.CODIGOESTACION into joined
                                             from j in joined.DefaultIfEmpty() //left join
                                             where col.CODIGOESTACION == primerNodo.codigoCTM
                                             select new TransferCodes
                                             {
                                                 id = tf.ID,
                                                 CEMetro = tf.CEMetro,
                                                 CELigero = tf.CELigero
                                             }
                                             ).Distinct().ToList();

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
                                                                      denominacion = mol.DENOMINACION,
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
                        //Buses
                        otherTransportTransfers.AddRange((from be in db.bus_estacion
                                                          join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                                          where primerNodo.lat + 0.0015 > be.lat && primerNodo.lat - 0.0015 < be.lat && primerNodo.lon + 0.0015 > be.lon && primerNodo.lon - 0.0015 < be.lon
                                                          select new NodeTransport
                                                          {
                                                              id = bol.OBJECTID,
                                                              codigoCTM = be.CODIGOESTACION,
                                                              codigoparada = be.CODIGOPARADA,
                                                              linea = bol.NUMEROLINEAUSUARIO,
                                                              sentido = bol.SENTIDO,
                                                              denominacion = bol.DENOMINACION,
                                                              ordenLinea = bol.NUMEROORDEN,
                                                              lat = be.lat,
                                                              lon = be.lon,
                                                              type = Models.Type.URBANO
                                                          }).ToList());

                        nodes.AddRange(otherTransportTransfers);
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
                        primerNodo = nodes[0];
                        otherStationCodes = (from tf in db.transbordos_ferroviarios
                                             join me in db.metro_estacion on tf.CEMetro equals me.CODIGOESTACION
                                             join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION into joined
                                             from j in joined.DefaultIfEmpty()
                                             where me.CODIGOCTMESTACIONREDMETRO == primerNodo.codigoCTM
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
                        //Buses
                        otherTransportTransfers.AddRange((from be in db.bus_estacion
                                                          join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                                          where primerNodo.lat + 0.0015 > be.lat && primerNodo.lat - 0.0015 < be.lat && primerNodo.lon + 0.0015 > be.lon && primerNodo.lon - 0.0015 < be.lon
                                                          select new NodeTransport
                                                          {
                                                              id = bol.OBJECTID,
                                                              codigoCTM = be.CODIGOESTACION,
                                                              codigoparada = be.CODIGOPARADA,
                                                              linea = bol.NUMEROLINEAUSUARIO,
                                                              sentido = bol.SENTIDO,
                                                              denominacion = bol.DENOMINACION,
                                                              ordenLinea = bol.NUMEROORDEN,
                                                              lat = be.lat,
                                                              lon = be.lon,
                                                              type = Models.Type.URBANO
                                                          }).ToList());
                        nodes.AddRange(otherTransportTransfers);
                        break;
                    case "M. LIGERO":
                        nodes = (from le in db.ligero_estacion
                                 join lol in db.ligero_orden_linea on le.CODIGOESTACION equals lol.CODIGOESTACION
                                 where denominacion == le.DENOMINACION
                                 select new NodeTransport
                                 {
                                     id = le.CODIGOESTACION,
                                     denominacion = le.DENOMINACION,
                                     codigoCTM = le.CODIGOCTMESTACIONREDMETRO,
                                     ordenLinea = lol.NUMEROORDEN,
                                     linea = lol.NUMEROLINEAUSUARIO,
                                     lat = le.lat,
                                     lon = le.lon,
                                     type = Models.Type.LIGERO
                                 }).ToList();

                        primerNodo = nodes[0];
                        otherStationCodes = (from tf in db.transbordos_ferroviarios
                                             join me in db.ligero_estacion on tf.CELigero equals me.CODIGOESTACION
                                             join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION into joined
                                             from j in joined.DefaultIfEmpty()
                                             where me.CODIGOCTMESTACIONREDMETRO == primerNodo.codigoCTM
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
                                                                      type = Models.Type.METRO
                                                                  }).ToList());
                            }

                        }

                        //Buses
                        otherTransportTransfers.AddRange((from be in db.bus_estacion
                                                          join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                                          where primerNodo.lat + 0.0015 > be.lat && primerNodo.lat - 0.0015 < be.lat && primerNodo.lon + 0.0015 > be.lon && primerNodo.lon - 0.0015 < be.lon
                                                          select new NodeTransport
                                                          {
                                                              id = bol.OBJECTID,
                                                              codigoCTM = be.CODIGOESTACION,
                                                              codigoparada = be.CODIGOPARADA,
                                                              linea = bol.NUMEROLINEAUSUARIO,
                                                              sentido = bol.SENTIDO,
                                                              denominacion = bol.DENOMINACION,
                                                              ordenLinea = bol.NUMEROORDEN,
                                                              lat = be.lat,
                                                              lon = be.lon,
                                                              type = Models.Type.URBANO
                                                          }).ToList());
                        nodes.AddRange(otherTransportTransfers);
                        break;

                    case "BUS":
                        nodes = (from be in db.bus_estacion
                                 join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                 where denominacion == be.DENOMINACION
                                 select new NodeTransport
                                 {
                                     id = bol.OBJECTID,
                                     codigoCTM = be.CODIGOESTACION,
                                     codigoparada = be.CODIGOPARADA,
                                     denominacion = be.DENOMINACION,
                                     linea = bol.NUMEROLINEAUSUARIO,
                                     ordenLinea = bol.NUMEROORDEN,
                                     sentido = bol.SENTIDO,
                                     lat = be.lat,
                                     lon = be.lon,
                                     type = Models.Type.URBANO
                                 }).Distinct().ToList();
                        break;
                }
            }
            return nodes;
        }

        public ActionResult FindPublicTransportPath(string Start, string End)
        {
            var start = Start.Split(',');
            var end = End.Split(',');
            using (var db = new TransportePublicoEntities())
            {
                List<NodeTransport> startNodes = GetNodes(start);
                List<NodeTransport> endNodes = GetNodes(end);
                PTPath(startNodes, endNodes);
                NodeTransport[] waypoints = { startNodes[0], endNodes[0] };

                return Json(new
                {
                    //Path = Session["Path"] as List<NodeTransport>,
                    PTransport = Session["PTransport"],
                    Waypoints = waypoints
                });
            }
        }


        public ActionResult GetItineroRoutes(FormCollection form)
        {
            var coordsString = form["coords"].Split(',');
            NodeTransport orig = new NodeTransport
            {
                lat = float.Parse(coordsString[1], CultureInfo.InvariantCulture),
                lon = float.Parse(coordsString[0], CultureInfo.InvariantCulture)
            };
            NodeTransport dest = new NodeTransport
            {
                lat = float.Parse(coordsString[3], CultureInfo.InvariantCulture),
                lon = float.Parse(coordsString[2], CultureInfo.InvariantCulture)
            };

            //GetItinero(orig, dest, Vehicle.Car);
            //GetItinero(orig, dest, Vehicle.Bicycle);
            //GetItinero(orig, dest, Vehicle.Pedestrian);

            return Json(new
            {
                Car = Session["Car"],
                Bicycle = Session["Bicycle"],
                Pedestrian = Session["Pedestrian"]
            });
        }


        void PTPath(List<NodeTransport> origNearList, List<NodeTransport> destNearList)
        {
            List<NodeTransport> closedSet = new List<NodeTransport>();
            List<NodeTransport> candidateNodes = new List<NodeTransport>();
            foreach (var origNode in origNearList) //BT?
            {
                if (destNearList.Any(destNode => origNode.linea == destNode.linea && origNode.type == destNode.type))
                {
                    candidateNodes.Add(origNode);
                }
            }

            if (candidateNodes.Count > 0) //Nodos que tienen línea directa
            {
                closedSet = BTDirectLines(candidateNodes, destNearList, closedSet, 0);
                if (closedSet.Count != 0)
                    Session["PTransport"] = closedSet;
                else
                    Astar(origNearList, destNearList, false);
            }
            else //nodos q no tienen línea directa
            {
                if (destNearList.TrueForAll(elem => elem.type == Models.Type.URBANO))
                {
                    var firstDest = destNearList.FirstOrDefault();
                    using (var db = new TransportePublicoEntities())
                        destNearList = (from be in db.bus_estacion
                                        join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                        where firstDest.linea == bol.NUMEROLINEAUSUARIO && firstDest.sentido == bol.SENTIDO && firstDest.ordenLinea >= bol.NUMEROORDEN
                                        orderby bol.NUMEROORDEN
                                        select new NodeTransport
                                        {
                                            id = bol.OBJECTID,
                                            codigoCTM = be.CODIGOESTACION,
                                            codigoparada = be.CODIGOPARADA,
                                            denominacion = be.DENOMINACION,
                                            linea = bol.NUMEROLINEAUSUARIO,
                                            ordenLinea = bol.NUMEROORDEN,
                                            sentido = bol.SENTIDO,
                                            lat = be.lat,
                                            lon = be.lon,
                                            type = Models.Type.URBANO
                                        }).ToList();
                    Astar(origNearList, destNearList, true);
                } else
                    Astar(origNearList, destNearList, false);
            }

            //Session["Path"] = closedSet;
        }




        List<NodeTransport> BTDirectLines(List<NodeTransport> origNodeList, List<NodeTransport> destNodeList, List<NodeTransport> sol, int step)
        {
            for (int i = 0; i < destNodeList.Count; i++)
            {
                if (step < origNodeList.Count) //factible
                {
                    if (destNodeList[i].linea == origNodeList[step].linea && destNodeList[i].type == origNodeList[step].type) //factible
                    {
                        var candidates = new List<NodeTransport>();
                        using (var db = new TransportePublicoEntities()) //recogemos datos
                        {
                            candidates = GetBTCandidatesList(origNodeList[step], destNodeList[i]);
                            if (sol.Count <= 0 && candidates.Count > 0)
                            {
                                sol = candidates;
                            }
                            else if (candidates.Count > 0 && candidates.Count < sol.Count && sol.Count > 0) //optimalidad
                            {
                                sol = candidates;
                            }
                        }
                        sol = BTDirectLines(origNodeList, destNodeList, sol, step + 1); //bt
                    }
                }
            }
            return sol;

        }
        List<NodeTransport> GetBTCandidatesList(NodeTransport nodeOrig, NodeTransport nodeDest)
        {
            List<NodeTransport> nodes = new List<NodeTransport>();
            using (var db = new TransportePublicoEntities())
            {
                switch (nodeOrig.type)
                {
                    case Models.Type.CERCANIAS:
                        if (nodeOrig.ordenLinea > nodeDest.ordenLinea)
                            nodes = (from ce in db.cercanias_estacion
                                     join col in db.cercanias_orden_linea on ce.CODIGOESTACION equals col.CODIGOESTACION
                                     where col.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                                     col.NUMEROORDEN <= nodeOrig.ordenLinea && col.NUMEROORDEN >= nodeDest.ordenLinea
                                     orderby col.NUMEROORDEN descending
                                     select new NodeTransport
                                     {
                                         id = col.OBJECTID,
                                         codigoCTM = ce.CODIGOESTACION,
                                         linea = col.NUMEROLINEAUSUARIO,
                                         denominacion = col.DENOMINACION,
                                         ordenLinea = col.NUMEROORDEN,
                                         lat = ce.lat,
                                         lon = ce.lon,
                                         type = Models.Type.CERCANIAS
                                     }).ToList();
                        else
                            nodes = (from ce in db.cercanias_estacion
                                     join col in db.cercanias_orden_linea on ce.CODIGOESTACION equals col.CODIGOESTACION
                                     where col.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                                     col.NUMEROORDEN >= nodeOrig.ordenLinea && col.NUMEROORDEN <= nodeDest.ordenLinea
                                     orderby col.NUMEROORDEN
                                     select new NodeTransport
                                     {
                                         id = col.OBJECTID,
                                         codigoCTM = ce.CODIGOESTACION,
                                         linea = col.NUMEROLINEAUSUARIO,
                                         denominacion = col.DENOMINACION,
                                         ordenLinea = col.NUMEROORDEN,
                                         lat = ce.lat,
                                         lon = ce.lon,
                                         type = Models.Type.CERCANIAS
                                     }).ToList();
                        break;
                    case Models.Type.METRO:
                        if (nodeOrig.ordenLinea > nodeDest.ordenLinea)
                            nodes = (from me in db.metro_estacion
                                     join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                     where mol.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                                     mol.NUMEROORDEN <= nodeOrig.ordenLinea && mol.NUMEROORDEN >= nodeDest.ordenLinea
                                     orderby mol.NUMEROORDEN descending
                                     select new NodeTransport
                                     {
                                         id = me.CODIGOESTACION,
                                         codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                         linea = mol.NUMEROLINEAUSUARIO,
                                         denominacion = me.DENOMINACION,
                                         ordenLinea = mol.NUMEROORDEN,
                                         lat = me.lat,
                                         lon = me.lon,
                                         type = Models.Type.METRO
                                     }).ToList();
                        else
                            nodes = (from me in db.metro_estacion
                                     join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                     where mol.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                                     mol.NUMEROORDEN >= nodeOrig.ordenLinea && mol.NUMEROORDEN <= nodeDest.ordenLinea
                                     orderby mol.NUMEROORDEN
                                     select new NodeTransport
                                     {
                                         id = me.CODIGOESTACION,
                                         codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                         linea = mol.NUMEROLINEAUSUARIO,
                                         denominacion = me.DENOMINACION,
                                         ordenLinea = mol.NUMEROORDEN,
                                         lat = me.lat,
                                         lon = me.lon,
                                         type = Models.Type.METRO
                                     }).ToList();
                        break;
                    case Models.Type.LIGERO:
                        if (nodeOrig.ordenLinea > nodeDest.ordenLinea)
                            nodes = (from me in db.ligero_estacion
                                     join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                     where mol.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                                     mol.NUMEROORDEN <= nodeOrig.ordenLinea && mol.NUMEROORDEN >= nodeDest.ordenLinea
                                     orderby mol.NUMEROORDEN descending
                                     select new NodeTransport
                                     {
                                         id = me.CODIGOESTACION,
                                         codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                         linea = mol.NUMEROLINEAUSUARIO,
                                         denominacion = me.DENOMINACION,
                                         ordenLinea = mol.NUMEROORDEN,
                                         lat = me.lat,
                                         lon = me.lon,
                                         type = Models.Type.LIGERO
                                     }).ToList();

                        else
                            nodes = (from me in db.ligero_estacion
                                     join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                     where mol.NUMEROLINEAUSUARIO == nodeOrig.linea &&
                                     mol.NUMEROORDEN >= nodeOrig.ordenLinea && mol.NUMEROORDEN <= nodeDest.ordenLinea
                                     orderby mol.NUMEROORDEN
                                     select new NodeTransport
                                     {
                                         id = me.CODIGOESTACION,
                                         codigoCTM = me.CODIGOCTMESTACIONREDMETRO,
                                         linea = mol.NUMEROLINEAUSUARIO,
                                         denominacion = me.DENOMINACION,
                                         ordenLinea = mol.NUMEROORDEN,
                                         lat = me.lat,
                                         lon = me.lon,
                                         type = Models.Type.LIGERO
                                     }).ToList();
                        break;
                    case Models.Type.URBANO:
                        if (nodeDest.sentido == nodeOrig.sentido)
                            nodes = (from be in db.bus_estacion
                                     join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                     where nodeOrig.ordenLinea < bol.NUMEROORDEN && nodeDest.ordenLinea > bol.NUMEROORDEN && nodeOrig.linea == bol.NUMEROLINEAUSUARIO && nodeOrig.sentido == bol.SENTIDO
                                     select new NodeTransport
                                     {
                                         id = bol.OBJECTID,
                                         codigoCTM = be.CODIGOESTACION,
                                         codigoparada = be.CODIGOPARADA,
                                         sentido = bol.SENTIDO,
                                         linea = bol.NUMEROLINEAUSUARIO,
                                         denominacion = be.DENOMINACION,
                                         ordenLinea = bol.NUMEROORDEN,
                                         lat = be.lat,
                                         lon = be.lon,
                                         type = Models.Type.URBANO
                                     }).ToList();
                        break;
                }

            }
            return nodes;
        }


        private void Astar(List<NodeTransport> origList, List<NodeTransport> destList, bool busStation) //https://www.youtube.com/watch?v=mZfyt03LDH4&list=PLFt_AvWsXl0cq5Umv3pMC9SPnKjfp9eGW&index=3&ab_channel=SebastianLague
        {
            Debug.Write("Nodos origen: ");
            foreach(var orig in origList)
            {
                Debug.WriteLine("\t" + orig.ToString());
            }
            Debug.WriteLine("\nNodos destino: ");
            foreach(var dest in destList)
            {
                Debug.WriteLine("\t" + dest.ToString());
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<NodeTransport> openSet = new List<NodeTransport>();
            HashSet<NodeTransport> closedSet = new HashSet<NodeTransport>();

            openSet.AddRange(origList);
            while (openSet.Count > 0)
            {
                NodeTransport currentNode = openSet[0];
                for (int i = 0; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost /*|| openSet[i].fCost == currentNode.fCost*/ && openSet[i].hCost < currentNode.hCost) //Condición optimalidad
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode); //Lo metemos en la lista cerrada
                Debug.WriteLine("Explorando nodo " + currentNode);
                if (destList.Contains(currentNode))
                {
                    RetracePath(currentNode, busStation, destList); //Hay q hacer traceback y recoger el path, ya q hay nodos q no necesitamos
                    sw.Stop();
                    Debug.WriteLine("Tiempo: " + sw.ElapsedMilliseconds / 1000 + " segundos");
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

                    double newMovementCostToNeighbour;

                    
                    if (currentNode.type != neighbour.type) //cambio de transporte
                        newMovementCostToNeighbour = currentNode.gCost + 10;
                    else if (currentNode.linea != neighbour.linea) //cambio de línea
                        newMovementCostToNeighbour = currentNode.gCost + 5;
                    else
                        newMovementCostToNeighbour = currentNode.gCost + 1;

                    double currentDistance = GetEuclideanDistance(currentNode, destList);
                    double neighbourDistance = GetEuclideanDistance(neighbour, destList);

                    if (currentDistance > neighbourDistance || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;

                        neighbour.hCost = neighbourDistance / 10;
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }
        }

        public double GetEuclideanDistance(NodeTransport node, List<NodeTransport> destNodeList)
        {
            double shortest = Double.MaxValue;
            double distance = 0;
            foreach (var destNode in destNodeList)
            {
                distance = GetEuclideanDistanceBetweenNodes(node, destNode);
                if (shortest > distance)
                    shortest = distance;
            }
            return distance;
        }

        private void RetracePath(NodeTransport end, bool busStation, List<NodeTransport> destList)
        {
            var path = new List<NodeTransport>();
            List<NodeTransport> restOfPath = new List<NodeTransport>();
            NodeTransport currentNode = end;
            while (currentNode != null)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            if (busStation && end != destList[destList.Count - 1])
            {
                restOfPath = destList.FindAll(elem => elem.ordenLinea > end.ordenLinea);
            }
            path.Reverse();
            path.AddRange(restOfPath);
            Session["PTransport"] = path;
        }


        private List<NodeTransport> GetNeighbours(NodeTransport node)
        {
            var type = node.type;
            List<NodeTransport> nodelist = new List<NodeTransport>();

            using (var db = new TransportePublicoEntities())
            {
                IEnumerable<NodeTransport> prevAndNextStations = new List<NodeTransport>();
                IEnumerable<NodeTransport> transferNodes = new List<NodeTransport>();
                IEnumerable<TransferCodes> otherStationCodes = new List<TransferCodes>();
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
                                               });
                        


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
                                         });
                        

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
                                             }).Distinct();

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
                                                                  }));
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
                                                                  }));
                            }

                        }



                        //Buses
                        otherTransportTransfers.AddRange((from be in db.bus_estacion
                                                          join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                                          where node.lat + 0.0015 > be.lat && node.lat - 0.0015 < be.lat && node.lon + 0.0015 > be.lon && node.lon - 0.0015 < be.lon
                                                          select new NodeTransport
                                                          {
                                                              id = bol.OBJECTID,
                                                              codigoCTM = be.CODIGOESTACION,
                                                              codigoparada = be.CODIGOPARADA,
                                                              linea = bol.NUMEROLINEAUSUARIO,
                                                              sentido = bol.SENTIDO,
                                                              denominacion = bol.DENOMINACION,
                                                              ordenLinea = bol.NUMEROORDEN,
                                                              lat = be.lat,
                                                              lon = be.lon,
                                                              type = Models.Type.URBANO
                                                          }));
                        nodelist.AddRange(prevAndNextStations);
                        nodelist.AddRange(transferNodes);
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
                                               });
                        

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
                                         });
                        

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
                                             }).Distinct();

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
                                                                  }));
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
                                                                  }));
                            }
                        }

                        //Buses
                        otherTransportTransfers.AddRange((from be in db.bus_estacion
                                                          join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                                          where node.lat + 0.0015 > be.lat && node.lat - 0.0015 < be.lat && node.lon + 0.0015 > be.lon && node.lon - 0.0015 < be.lon
                                                          select new NodeTransport
                                                          {
                                                              id = bol.OBJECTID,
                                                              codigoCTM = be.CODIGOESTACION,
                                                              codigoparada = be.CODIGOPARADA,
                                                              denominacion = bol.DENOMINACION,
                                                              linea = bol.NUMEROLINEAUSUARIO,
                                                              sentido = bol.SENTIDO,
                                                              ordenLinea = bol.NUMEROORDEN,
                                                              lat = be.lat,
                                                              lon = be.lon,
                                                              type = Models.Type.URBANO
                                                          }));
                        nodelist.AddRange(otherTransportTransfers);
                        nodelist.AddRange(transferNodes);
                        nodelist.AddRange(prevAndNextStations);
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
                                               });
                        


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
                                         });
                        

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
                                             }).Distinct();

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
                                                                  }));
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
                                                                  }));
                            }
                        }
                        //Buses
                        otherTransportTransfers.AddRange((from be in db.bus_estacion
                                                          join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                                          where node.lat + 0.0015 > be.lat && node.lat - 0.0015 < be.lat && node.lon + 0.0015 > be.lon && node.lon - 0.0015 < be.lon
                                                          select new NodeTransport
                                                          {
                                                              id = bol.OBJECTID,
                                                              denominacion = bol.DENOMINACION,
                                                              codigoCTM = be.CODIGOESTACION,
                                                              codigoparada = be.CODIGOPARADA,
                                                              linea = bol.NUMEROLINEAUSUARIO,
                                                              sentido = bol.SENTIDO,
                                                              ordenLinea = bol.NUMEROORDEN,
                                                              lat = be.lat,
                                                              lon = be.lon,
                                                              type = Models.Type.URBANO
                                                          }));
                        nodelist.AddRange(otherTransportTransfers);
                        nodelist.AddRange(prevAndNextStations);
                        nodelist.AddRange(transferNodes);
                        break;
                    case Models.Type.URBANO:
                        //Se escogerán estaciones de otras líneas con mismo nombre y estación contigua del sentido en el q estemos + transbordos ferroviarios dado un rango de 300m aprox
                        prevAndNextStations = (from be in db.bus_estacion
                                               join bol in db.bus_orden_linea on be.CODIGOESTACION equals bol.CODIGOESTACION
                                               where node.linea == bol.NUMEROLINEAUSUARIO && bol.SENTIDO == node.sentido && node.ordenLinea == bol.NUMEROORDEN + 1
                                               select new NodeTransport
                                               {
                                                   id = bol.OBJECTID,
                                                   codigoCTM = be.CODIGOESTACION,
                                                   denominacion = be.DENOMINACION,
                                                   codigoparada = be.CODIGOPARADA,
                                                   linea = bol.NUMEROLINEAUSUARIO,
                                                   sentido = bol.SENTIDO,
                                                   ordenLinea = bol.NUMEROORDEN,
                                                   lat = be.lat,
                                                   lon = be.lon,
                                                   type = Models.Type.URBANO
                                               });


                        otherTransportTransfers.AddRange((from me in db.metro_estacion
                                                   join mol in db.metro_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                                   where node.lat + 0.003 > me.lat && node.lat - 0.003 < me.lat && node.lon + 0.003 > me.lon && node.lon - 0.003 < me.lon
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
                                                   })
                                                   .Union(
                                                        from ce in db.cercanias_estacion
                                                        join col in db.cercanias_orden_linea on ce.CODIGOESTACION equals col.CODIGOESTACION
                                                        where node.lat + 0.004 > ce.lat && node.lat - 0.004 < ce.lat && node.lon + 0.004 > ce.lon && node.lon - 0.004 < ce.lon
                                                        select new NodeTransport
                                                        {
                                                            id = col.OBJECTID,
                                                            codigoCTM = ce.CODIGOESTACION,
                                                            denominacion = col.DENOMINACION,
                                                            linea = col.NUMEROLINEAUSUARIO,
                                                            ordenLinea = col.NUMEROORDEN,
                                                            lat = ce.lat,
                                                            lon = ce.lon,
                                                            type = Models.Type.CERCANIAS
                                                        }).Union(
                                                            from me in db.ligero_estacion
                                                            join mol in db.ligero_orden_linea on me.CODIGOESTACION equals mol.CODIGOESTACION
                                                            where node.lat + 0.0015 > me.lat && node.lat - 0.0015 < me.lat && node.lon + 0.0015 > me.lon && node.lon - 0.0015 < me.lon
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
                                                            }));
                        nodelist.AddRange(otherTransportTransfers);
                        nodelist.AddRange(prevAndNextStations);


                        break;
                }
                return nodelist;
            }
        }

        private double GetEuclideanDistanceBetweenNodes(NodeTransport orig, NodeTransport dest)
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
            else if (vehicle.Equals(Vehicle.Bicycle))
            {
                path = ControllerContext.HttpContext.Server.MapPath(@"~/Content/Maps/serialized-madrid-bicycle.routerdb");
            }

            using (var stream = new FileInfo(path).OpenRead())
            {
                routerDb = RouterDb.Deserialize(stream, RouterDbProfile.NoCache);
                var router = new Router(routerDb);
                var profile = vehicle.Fastest();
                //https://www.google.com/search?q=build+x64+visual+studio&sxsrf=APq-WBuhyq-UCQbZrwKAUD16nxI7zj9F2A%3A1650654811681&ei=W_5iYtigKZe7lwTc06OABA&ved=0ahUKEwiYusmtsKj3AhWX3YUKHdzpCEAQ4dUDCA8&uact=5&oq=build+x64+visual+studio&gs_lcp=Cgxnd3Mtd2l6LXNlcnAQAzIHCCMQsAMQJzIHCCMQsAMQJzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwAzIHCAAQRxCwA0oECEEYAEoECEYYAFAAWABglhNoAnAAeACAAQCIAQCSAQCYAQDIAQrAAQEastar&sclient=gws-wiz-serp
                //https://stackoverflow.com/questions/18892159/why-cant-i-set-asp-net-mvc-4-project-to-be-x64

                //Error dimensiones matriz //cambiar compilador a x64
                //https://social.msdn.microsoft.com/Forums/vstudio/en-US/5850560d-a4a3-4cf5-be9e-b71026d1e175/systemoutofmemoryexception-array-dimensions-exceeded-supported-range-on-dataset?forum=vbgeneral
                var start = router.Resolve(profile, Convert.ToSingle(orig.lat), Convert.ToSingle(orig.lon));

                var end = router.Resolve(profile, Convert.ToSingle(dest.lat), Convert.ToSingle(dest.lon));

                var route = router.Calculate(profile, start, end);

                if (vehicle.Equals(Vehicle.Pedestrian))
                {
                    Session["Pedestrian"] = new RouteInfo()
                    {
                        Shape = route.Shape,
                        Time = route.TotalTime,
                        Distance = route.TotalDistance,
                        Emissions = 0
                    };
                }
                else if (vehicle.Equals(Vehicle.Car))
                {
                    Session["Car"] = new RouteInfo()
                    {
                        Shape = route.Shape,
                        Time = route.TotalTime,
                        Distance = route.TotalDistance

                    };
                }
                else if (vehicle.Equals(Vehicle.Bicycle))
                {
                    Session["Bicycle"] = new RouteInfo()
                    {
                        Shape = route.Shape,
                        Time = route.TotalTime,
                        Distance = route.TotalDistance,
                        Emissions = 0
                    };
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

            //TODO: falta por añadir más estaciones
            using (var db = new TransportePublicoEntities())
            {
                var stations = (from m in db.metro_estacion
                                select "METRO - " + m.DENOMINACION).Distinct().Union(
                                     from c in db.cercanias_orden_linea
                                     select "CERCANIAS - " + c.DENOMINACION).Distinct().Union(
                                         from l in db.ligero_estacion
                                         select "M. LIGERO - " + l.DENOMINACION).Distinct().Union(
                                             from b in db.bus_estacion
                                             join bol in db.bus_orden_linea on b.CODIGOESTACION equals bol.CODIGOESTACION
                                             select "BUS - " + b.DENOMINACION).Distinct();


                return Json(new
                {
                    Stations = stations.ToList()
                }, JsonRequestBehavior.AllowGet); ;
            }
        }
    }
}