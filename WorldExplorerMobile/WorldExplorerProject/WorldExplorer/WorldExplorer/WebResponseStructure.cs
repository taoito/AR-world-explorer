using System.Runtime.Serialization;
using System.Collections.Generic;

namespace WorldExplorer.DataStructure
{
    [DataContract]
    public class Coordinate
    {
        [DataMember]
        public double lat;
        [DataMember]
        public double lng;
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
    public class Event
    {
        [DataMember]
        public string dummyString;
    }

    [DataContract]
    public class MyWebResponse
    {
        [DataMember]
        public List<Location> places;
        [DataMember]
        public List<Event> events;
    }

    [DataContract]
    public class Message
    {

        [DataMember]
        public string text { get; set; }
        [DataMember]
        public int code { get; set; }
        [DataMember]
        public string version { get; set; }
    }

    [DataContract]
    public class Category
    {
        [DataMember]
        public string category_filter { get; set; }
        [DataMember]
        public string search_url { get; set; }
        [DataMember]
        public string name { get; set; }
    }

    [DataContract]
    public class Neighborhood
    {
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string name { get; set; }
    }

    [DataContract]
    public class Review
    {
        [DataMember]
        public string rating_img_url_small { get; set; }
        [DataMember]
        public string user_photo_url_small { get; set; }
        [DataMember]
        public string rating_img_url { get; set; }
        [DataMember]
        public int rating { get; set; }
        [DataMember]
        public string user_url { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string mobile_uri { get; set; }
        [DataMember]
        public string text_excerpt { get; set; }
        [DataMember]
        public string user_photo_url { get; set; }
        [DataMember]
        public string date { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public string id { get; set; }
    }

    [DataContract]
    public class Business
    {
        [DataMember]
        public string rating_img_url { get; set; }
        [DataMember]
        public string country_code { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public bool is_closed { get; set; }
        [DataMember]
        public string city { get; set; }
        [DataMember]
        public string mobile_url { get; set; }
        [DataMember]
        public int review_count { get; set; }
        [DataMember]
        public string zip { get; set; }
        [DataMember]
        public string state { get; set; }
        [DataMember]
        public double latitude { get; set; }
        [DataMember]
        public string rating_img_url_small { get; set; }
        [DataMember]
        public string address1 { get; set; }
        [DataMember]
        public string address2 { get; set; }
        [DataMember]
        public string address3 { get; set; }
        [DataMember]
        public string phone { get; set; }
        [DataMember]
        public string state_code { get; set; }
        [DataMember]
        public List<Category> categories { get; set; }
        [DataMember]
        public string photo_url { get; set; }
        [DataMember]
        public double distance { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public List<Neighborhood> neighborhoods { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public string country { get; set; }
        [DataMember]
        public double avg_rating { get; set; }
        [DataMember]
        public double longitude { get; set; }
        [DataMember]
        public string nearby_url { get; set; }
        [DataMember]
        public List<Review> reviews { get; set; }
        [DataMember]
        public string photo_url_small { get; set; }
    }

    [DataContract]
    public class RootObject
    {
        [DataMember]
        public Message message { get; set; }
        [DataMember]
        public List<Business> businesses { get; set; }
    }
}