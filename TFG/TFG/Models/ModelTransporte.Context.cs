﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class TransportePublicoEntities : DbContext
    {
        public TransportePublicoEntities()
            : base("name=TransportePublicoEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<metro_estacion> metro_estacion { get; set; }
        public virtual DbSet<metro_orden_linea> metro_orden_linea { get; set; }
    
        public virtual ObjectResult<sp_get_transfers_from_line_Result> sp_get_transfers_from_line(string line)
        {
            var lineParameter = line != null ?
                new ObjectParameter("line", line) :
                new ObjectParameter("line", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<sp_get_transfers_from_line_Result>("sp_get_transfers_from_line", lineParameter);
        }
    }
}
