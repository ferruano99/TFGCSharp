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
    
    public partial class cercanias_estacion
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public cercanias_estacion()
        {
            this.transbordos_ferroviarios = new HashSet<transbordos_ferroviarios>();
            this.cercanias_orden_linea = new HashSet<cercanias_orden_linea>();
        }
    
        public int CODIGOESTACION { get; set; }
        public Nullable<double> lon { get; set; }
        public Nullable<double> lat { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<transbordos_ferroviarios> transbordos_ferroviarios { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<cercanias_orden_linea> cercanias_orden_linea { get; set; }
    }
}