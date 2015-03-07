using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorldExplorerServer.Controllers
{

    #region JSON Data parsing

    public class Friendlist
    {
        public List<FacebookUser> data { get; set; }
        public Paging paging { get; set; }
    }

    public class FacebookFriendEventDetail
    {
        public string name { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string timezone { get; set; }
        public string location { get; set; }
        public string id { get; set; }
        public string rsvp_status { get; set; }
    }

    public class Paging
    {
        public string previous { get; set; }
        public string next { get; set; }
    }

    public class FacebookFriendEvent
    {
        public List<FacebookFriendEventDetail> data { get; set; }
        public Paging paging { get; set; }
    }


    // Owner of that event
    public class Owner
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    // Location of an event
    public class Venue
    {
        public string id { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zip { get; set; }
    }

    // Information of an event
    public class FacebookEventData
    {
        public string id { get; set; }
        public Owner owner { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string timezone { get; set; }
        public bool is_date_only { get; set; }
        public string location { get; set; }
        public Venue venue { get; set; }
        public string privacy { get; set; }
        public string updated_time { get; set; }
    }

    // Address of the location where the event is happening
    public class FacebookEventLocationAddress
    {
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zip { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    // Information of the location where the event is happening
    public class FacebookEventLocation
    {
        public int checkins { get; set; }
        public bool is_community_page { get; set; }
        public bool is_published { get; set; }
        public FacebookEventLocationAddress location { get; set; }
        public string phone { get; set; }
        public int talking_about_count { get; set; }
        public string website { get; set; }
        public int were_here_count { get; set; }
        public string category { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string link { get; set; }
        public int likes { get; set; }
    }
    #endregion


}