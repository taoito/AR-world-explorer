using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Device.Location;

namespace WorldExplorer.DataStructure
{
    public class FacebookEventData
    {
        public string name;
        public GeoCoordinate geoCoordinate;
        public bool isValid;

        public FacebookEventData()
        {
            this.name = null;
            geoCoordinate = new GeoCoordinate();
            isValid = false;
        }
    }

    [DataContract]
    public class FacebookLocation
    {
        [DataMember(Name = "latitude")]
        public double latitude;
        [DataMember(Name = "longitude")]
        public double longitude;
    }

    [DataContract]
    public class LocationId {
        [DataMember(Name = "location")]
        public FacebookLocation location;
        [DataMember(Name = "id")]
        public string id;
    }
    // Data structure for getting events information
    [DataContract]
    public class Venue 
    {
        [DataMember(Name = "id")]
        public string id;
        [DataMember(Name = "latitude")]
        public double latitude;
        [DataMember(Name = "longitude")]
        public double longitude;
    }
    
    [DataContract]
    public class EventFacebook
    {
        [DataMember(Name = "venue")]
        public Venue venue;
        [DataMember(Name = "name")]
        public string name;

    }

    // Data structure for search events on facebook
    [DataContract]
    public class EventId
    {
        [DataMember(Name = "id")]
        public string id;
        [DataMember(Name = "name")]
        public string name;
        [DataMember(Name = "start_time")]
        public string start_time;
    }

    [DataContract]
    public class Page
    {
        [DataMember(Name = "previous")]
        public string previous;
        [DataMember(Name = "next")]
        public string next;
    }

    [DataContract]
    public class FacebookResponse
    {
        [DataMember(Name = "data")]
        public List<EventId> data;
        [DataMember(Name = "paging")]
        public Page page ;
    }
}