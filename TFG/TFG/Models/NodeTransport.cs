using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TFG.Models
{
    public class NodeTransport
    {
        //public NodeTransport(double _lat, double _lon)
        //{
        //    lat = _lat;
        //    lon = _lon;
        //}
        public int? id { get; set; } //codigoestacion
        public string linea { get; set; }
        public string denominacion { get; set; }
        public int tipo { get; set; } //si es bus metro tal
        public double? lat { get; set; }
        public double? lon { get; set; }
        public double gCost { get; set; }
        public double hCost { get; set; }
        public double fCost
        {
            get
            {
                return gCost + hCost;
            }
        }
    }
}