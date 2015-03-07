using System.Runtime.Serialization;
using System.Collections.Generic;

namespace WorldExplorer.DataStructure
{

    [DataContract]
    public class BasicCloudResponse
    {
        [DataMember]
        public List<Location> places;
        [DataMember]
        public string error;
    }

    [DataContract]
    public class MoreDetailsResponse
    {
        [DataMember]
        public List<MoreDetailsMobileResponse> detailPlaces;
        [DataMember]
        public List<MoreDetailsMobileResponse> detailEvents;
    }

    [DataContract]
    public class Location
    {
        [DataMember]
        public string id;
        [DataMember]
        public Coordinate location;
        [DataMember]
        public string name;

    }

    [DataContract]
    public class Coordinate
    {
        [DataMember]
        public double lat;
        [DataMember]
        public double lng;
    }

    [DataContract]
    public class MoreDetailsMobileResponse
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public double Lat { get; set; }
        [DataMember]
        public double Lng { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Address { get; set; }
        [DataMember]
        public string PhoneNumber { get; set; }
        [DataMember]
        public double Rating { get; set; }
        [DataMember]
        public List<string> OpeningHours { get; set; }
        [DataMember]
        public string Website { get; set; }
        [DataMember]
        public List<string> Reviews { get; set; }
        [DataMember]
        public int PublicCheckins { get; set; }
        [DataMember]
        public string FriendsCheckins { get; set; }
        [DataMember]
        public string PhotoString { get; set; }
        [DataMember]
        public string StreetViewString { get; set; }
        [DataMember]
        public string EventId { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string HostName { get; set; }
        [DataMember]
        public string StartTime { get; set; }
        [DataMember]
        public string EndTime { get; set; }
        [DataMember]
        public List<string> AttendantList { get; set; }
        [DataMember]
        public List<string> AttendantPicList { get; set; }

    }

    [DataContract]
    public class FacebookEventMobileResponse
    {
        [DataMember]
        public string EventId { get; set; }
        [DataMember]
        public double Lat { get; set; }
        [DataMember]
        public double Lng { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string HostName { get; set; }
        [DataMember]
        public string StartTime { get; set; }
        [DataMember]
        public string EndTime { get; set; }
        [DataMember]
        public string Address { get; set; }
        [DataMember]
        public List<string> AttendantList { get; set; }
        [DataMember]
        public List<string> AttendantPicList { get; set; }
        [DataMember]
        public string PhotoString { get; set; }
        [DataMember]
        public string StreetViewString { get; set; }
    }
}