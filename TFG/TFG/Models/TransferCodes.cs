using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TFG.Models
{
    public class TransferCodes
    {
        public int id { get; set; }
        public int? CEMetro { get; set; }
        public int? CECercanias { get; set; }
        public int? CELigero { get; set; }
    }
}