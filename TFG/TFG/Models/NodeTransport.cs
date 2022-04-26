using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TFG.Models
{

    //public enum TransportType
    //{
    //    Metro, BusUrbano, BusInterurbano, Cercanias, MetroLigero, OnFoot
    //}

    public class NodeTransport
    {
        public int? id { get; set; } //codigoestacion
        public string linea { get; set; }
        public int ordenLinea { get; set; }
        public string denominacion { get; set; }
        //public TransportType tipo { get; set; } //si es bus metro tal
        public double? lat { get; set; }
        public double? lon { get; set; }
        public double gCost { get; set; }
        public double hCost { get; set; }
        public NodeTransport parent { get; set; }
        public double fCost
        {
            get
            {
                return gCost + hCost;
            }
        }
    }
}