using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TFG.Models
{

    public enum Type
    {
        METRO = 1,
        CERCANIAS = 2,
        LIGERO = 3,
        INTERURBANO = 4,
        URBANO = 5
    }

    public class NodeTransport
    {
        public int? id { get; set; } //codigoestacion
        public int? codigoCTM { get; set; }
        public string linea { get; set; }
        public int? ordenLinea { get; set; }
        public string denominacion { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }
        public double gCost { get; set; }
        public double hCost { get; set; }
        public NodeTransport parent { get; set; }
        public Type type { get; set; }
        public double fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                NodeTransport nt = (NodeTransport)obj;
                return id == nt.id && denominacion == nt.denominacion;
            }
        }
    }

}