//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TFG.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class bus_orden_linea
    {
        public int OBJECTID { get; set; }
        public string NUMEROLINEAUSUARIO { get; set; }
        public int SENTIDO { get; set; }
        public int CODIGOESTACION { get; set; }
        public int NUMEROORDEN { get; set; }
        public string DENOMINACION { get; set; }
        public string MUNICIPIO { get; set; }
        public double LONGITUDTRAMOANTERIOR { get; set; }
        public Nullable<double> VELOCIDADTRAMOANTERIOR { get; set; }
    
        public virtual bus_estacion bus_estacion { get; set; }
    }
}
