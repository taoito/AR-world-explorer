using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ARWebRole.Models
{
    public class InfoQueueEvent
    {
        public List<FacebookEventTable> newFacebookEvents = new List<FacebookEventTable>();
    }

    public class AzureDetailEventResult
    {
        public List<FacebookEventMobileResponse> detailEvents = new List<FacebookEventMobileResponse>();
    }

    public class AzureSearchEventResult
    {
        public List<Place> places = new List<Place>();
    }

    public class FacebookEventMobileResponse
    {
        public string EventId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string HostName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Address { get; set; }
        public List<string> AttendantList { get; set; }
        public List<string> AttendantPicList { get; set; }
        public string PhotoString { get; set; }
        public string StreetViewString { get; set; }
    }
}