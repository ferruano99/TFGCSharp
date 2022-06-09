using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TFG.Models
{
    public class RouteInfo
    {
        public Itinero.LocalGeo.Coordinate[] Shape;
        public float Distance;
        public float Emissions;
        public float Time;

    }
}