using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorldExplorerServer.Models
{
    public class Place
    {
        public String id { get; set; }
        public Location location { get; set; }
        public String name { get; set; }

        public Place(String id, Location location, String name)
        {
            this.id = id;
            this.location = location;
            this.name = name;
        }
       
    }

    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }

        public Location(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
        }
    }
}