using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ARWebRole.Models
{
    public class InfoQueueItem
    {
        public List<GooglePlaceTable> newGooglePlaces = new List<GooglePlaceTable>();
    }

    public class AzureDetailResult
    {
        public List<GooglePlaceMobileResponse> detailPlaces = new List<GooglePlaceMobileResponse>();
    }

    public class AzureSearchResult
    {
        public List<Place> places = new List<Place>();
    }

    public class GoogleResponse
    {
        public List<string> html_attributions { get; set; }
        public List<GoogleResult> results { get; set; }
        public string status { get; set; }
    }

    public class GoogleResult
    {
        public Geometry geometry { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public double rating { get; set; }
        public string reference { get; set; }
        public List<string> types { get; set; }
    }

    public class GooglePlaceMobileResponse
    {
        public string Id { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Name { get; set; }
        public string Types { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public double Rating { get; set; }
        public List<string> OpeningHours { get; set; }
        public string Website { get; set; }
        public List<string> Reviews { get; set; }
        public int PublicCheckins { get; set; }
        public string FriendsCheckins { get; set; }
        public string PhotoString { get; set; }
        public string StreetViewString { get; set; }
    }
}