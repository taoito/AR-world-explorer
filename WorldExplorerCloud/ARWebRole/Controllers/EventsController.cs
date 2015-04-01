using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using ARWebRole.Models;
using ARWebRole.Utilities;
using Facebook;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ARWebRole.Controllers
{
    public class EventsController : ApiController
    {
        private CloudTable EventsTable;
        private CloudQueue moreInfoEventQueue;

        public EventsController()
        {
            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();
            EventsTable = tableClient.GetTableReference("EventsTable");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            moreInfoEventQueue = queueClient.GetQueueReference("moreinfoeventqueue");
        }

        //Retrieve a Facebook Event entry from the Azure Table
        private FacebookEventTable FindEvent(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<FacebookEventTable>(partitionKey, rowKey);
            var retrievedResult = EventsTable.Execute(retrieveOperation);
            var fbEvent = retrievedResult.Result as FacebookEventTable;
            return fbEvent;
        }

        public AzureDetailEventResult GetDetails(string eventId, string userId)
        {
            AzureDetailEventResult azureDetailResult = new AzureDetailEventResult();

            FacebookEventTable detailEvent = new FacebookEventTable();
            detailEvent = FindEvent(userId, eventId);

            FacebookEventMobileResponse detailMobileResponse = new FacebookEventMobileResponse();

            if (detailEvent != null)
            {
                detailMobileResponse.EventId = detailEvent.Id;
                detailMobileResponse.Lat = detailEvent.Lat;
                detailMobileResponse.Lng = detailEvent.Lng;
                detailMobileResponse.Name = detailEvent.Name;
                detailMobileResponse.Description = detailEvent.Description;
                detailMobileResponse.HostName = detailEvent.HostName;
                detailMobileResponse.StartTime = detailEvent.StartTime;
                detailMobileResponse.EndTime = detailEvent.EndTime;
                detailMobileResponse.Address = detailEvent.Address;
                detailMobileResponse.AttendantList = JsonConvert.DeserializeObject<List<string>>(detailEvent.AttendantList);
                detailMobileResponse.AttendantPicList = JsonConvert.DeserializeObject<List<string>>(detailEvent.AttendantPicList);
                for (int i = 0; i < detailMobileResponse.AttendantPicList.Count; i++)
                {
                    detailMobileResponse.AttendantPicList[i] = ImageDownload(detailMobileResponse.AttendantPicList[i]);
                }
  
                detailMobileResponse.PhotoString = ImageDownload(detailEvent.PhotoBlobRef);
                detailMobileResponse.StreetViewString = ImageDownload(detailEvent.StreetViewBlobRef);

                azureDetailResult.detailEvents.Add(detailMobileResponse);
            }
            return azureDetailResult;
        }

        private string ImageDownload(string url)
        {
            if ((url != null) && !url.Equals(""))
            {

                Byte[] barray;
                string base64String;

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
                HttpResponseMessage httpResponse = client.GetAsync(url).Result;
                barray = httpResponse.Content.ReadAsByteArrayAsync().Result;
                base64String = Convert.ToBase64String(barray);

                return base64String;
            }
            else
            {
                return "";
            }
        }
        
        public Object GetList(double lat, double lon, int radius, string userId, string accessToken)
        {
            Dictionary<string, FacebookLocation> locationDictionary = new Dictionary<string, FacebookLocation>();

            Dictionary<string, Place> placeDictionary = new Dictionary<string, Place>();

            Dictionary<string, FacebookEvent> eventDictionary = new Dictionary<string, FacebookEvent>();

            //EventsSearchResult eventResult = new EventsSearchResult();
            AzureSearchEventResult eventResult = new AzureSearchEventResult();
            FacebookSearchResults result;

            Stopwatch sw = new Stopwatch();

            try
            {
                var fb = new FacebookClient(accessToken);

                sw.Start();
                result = fb.Get<FacebookSearchResults>("fql",
                    //dynamic result = fb.Get("fql",
                                            new
                                            {
                                                q = new
                                                {
                                                    event_info = "SELECT name, description, start_time, end_time, eid, venue, location, pic, host FROM event WHERE eid IN (SELECT eid FROM event WHERE eid IN (SELECT eid FROM event_member WHERE uid IN ( SELECT uid2 FROM friend WHERE uid1 = me())))",
                                                    event_venue = "SELECT name, username, page_id, location  FROM page WHERE page_id IN  (SELECT venue.id FROM #event_info)",

                                                }
                                            });
                sw.Stop();
                System.Diagnostics.Debug.WriteLine("Elapsed time: {0}", sw.Elapsed);
            }
            catch (FacebookOAuthException)
            {
                Console.WriteLine("oauth exception occurred");
                return new { error =  "OAuthException"};
            }
            catch (FacebookApiLimitException)
            {
                Console.WriteLine("api limit exception occurred");
                return new { error = "FacebookApiLimitException" };
            }
            catch (FacebookApiException)
            {
                Console.WriteLine("other general facebook api exception");
                return new { error = "FacebookApiLimitException" };
            }
            catch (Exception)
            {
                Console.WriteLine("non-facebook exception raised");
                return new { error = "UnhandledException" };
            }

            FacebookSearchResult event_info = new FacebookSearchResult();
            FacebookSearchResult event_venue = new FacebookSearchResult();

            foreach (FacebookSearchResult fsr in result.data)
            {
                switch (fsr.name)
                {
                    case "event_info":
                        event_info = fsr;
                        break;
                    case "event_venue":
                        event_venue = fsr;
                        break;
                    default:
                        break;
                }
            }

            IList<dynamic> events = event_info.fql_result_set;

            IList<dynamic> venues = event_venue.fql_result_set;

            Stopwatch sw2 = new Stopwatch();

            sw2.Start();

            // Parsing the location pages that the events can be at
            for (int i = 0; i < venues.Count; i++)
            {
                // Only get locations with latitude and longitude

                JsonObject venue = (JsonObject)venues[i];

                if ((!venue.ContainsKey("page_id")) || (!venue.ContainsKey("location")))
                    continue;

                JsonObject venueLocation = (JsonObject)venue["location"];

                if ((!venueLocation.ContainsKey("longitude")) || (!venueLocation.ContainsKey("latitude")))
                    continue;

                FacebookLocation el = new FacebookLocation();
                el.id = venue["page_id"].ToString();
                el.latitude = Convert.ToDouble(venueLocation["latitude"]);
                el.longitude = Convert.ToDouble(venueLocation["longitude"]);
                el.country = venueLocation.ContainsKey("country") ? (string)venueLocation["country"] : "";
                el.state = venueLocation.ContainsKey("state") ? (string)venueLocation["state"] : "";
                el.street = venueLocation.ContainsKey("street") ? (string)venueLocation["street"] : "";
                el.zip = venueLocation.ContainsKey("zip") ? (string)venueLocation["zip"] : "";

                locationDictionary.Add(venue["page_id"].ToString(), el);
            }

            for (int i = 0; i < events.Count; i++)
            {
                JsonObject currEvent = (JsonObject)events[i];

                if ((!currEvent.ContainsKey("name")) || (!currEvent.ContainsKey("eid")) ||
                        (!currEvent.ContainsKey("venue")))
                    continue;

                FacebookEvent fe = new FacebookEvent();
                fe.name = (string)currEvent["name"];
                fe.eid = events[i].eid.ToString();
                fe.start_time = currEvent.ContainsKey("start_time") ? (string)currEvent["start_time"] : "";
                fe.end_time = currEvent.ContainsKey("end_time") ? (string)currEvent["end_time"] : "";
                fe.description = currEvent.ContainsKey("description") ? (string)currEvent["description"] : "";
                fe.pic = currEvent.ContainsKey("pic") ? (string)currEvent["pic"] : "";
                fe.host = currEvent.ContainsKey("host") ? (string)currEvent["host"] : "";

                // Filtering events based on start time and end time
                string today = DateTime.Today.ToString("yyyy-MM-dd");

                string severalDaysAfter = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

                string severalDaysAgo = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");

                if (fe.end_time != null)
                {
                    if (String.Compare(today, fe.end_time.Substring(0, 10)) > 0)
                        continue;
                }

                // Check if its start time is in desirable range
                if (fe.start_time != null)
                {
                    if (String.Compare(severalDaysAgo, fe.start_time.Substring(0, 10)) > 0)
                        continue;
                    if (String.Compare(severalDaysAfter, fe.start_time.Substring(0, 10)) < 0)
                        continue;

                }

                FacebookLocation el = new FacebookLocation();
                JsonObject eventVenue;

                try
                {
                    eventVenue = (JsonObject)currEvent["venue"];
                }
                catch (InvalidCastException)
                {
                    Console.WriteLine("Null venue, skip");

                    continue;
                }
                // If this event already has latitude and longitude information
                if (eventVenue.ContainsKey("latitude") && eventVenue.ContainsKey("longitude"))
                {
                    el.latitude = Convert.ToDouble(eventVenue["latitude"]);
                    el.longitude = Convert.ToDouble(eventVenue["longitude"]);
                    el.id = eventVenue.ContainsKey("id") ? eventVenue["id"].ToString() : "";
                    el.country = eventVenue.ContainsKey("country") ? (string)eventVenue["country"] : "";
                    el.state = eventVenue.ContainsKey("state") ? (string)eventVenue["state"] : "";
                    el.street = eventVenue.ContainsKey("street") ? (string)eventVenue["street"] : "";
                    el.zip = eventVenue.ContainsKey("zip") ? (string)eventVenue["zip"] : "";
                }
                else if (eventVenue.ContainsKey("id"))
                {
                    // Get it from location page id
                    if (locationDictionary.ContainsKey(eventVenue["id"].ToString()))
                    {
                        el = locationDictionary[eventVenue["id"].ToString()];
                    }
                    else
                    {
                        continue;
                    }

                }
                else
                {
                    // Just skip it if we can't get location's long and lat
                    continue;
                }

                fe.venue = el;
                eventDictionary.Add(fe.eid, fe);
            }

            InfoQueueEvent infoQueueEvent = new InfoQueueEvent();

            foreach (string eventId in eventDictionary.Keys)
            {
                FacebookEvent fe = eventDictionary[eventId];
                if (Distance.getDistance(lat, lon, fe.venue.latitude,
                                            fe.venue.longitude) < radius) {
                    //Compile results to return to Mobile Client
                    eventResult.places.Add(new Place(fe.eid, new Location(fe.venue.latitude, fe.venue.longitude), fe.name));

                    //Compile into a message to put into Azure More Info Event Queue, for AR Worker Role to pick up
                    FacebookEventTable fbEvent = new FacebookEventTable(userId, fe.eid, fe.venue.latitude, fe.venue.longitude, fe.name, 
                                               fe.description, fe.host, fe.venue.street, fe.start_time, fe.end_time, fe.pic, accessToken);
                    infoQueueEvent.newFacebookEvents.Add(fbEvent); 
                }
            }

            sw2.Stop();
            System.Diagnostics.Debug.WriteLine("Elapsed time: {0}", sw2.Elapsed);

            //Push message into Azure More Info Queue, for AR Worker Role to pick up
            string infoQueueMessageStr = JsonConvert.SerializeObject(infoQueueEvent);
            var infoQueueMessage = new CloudQueueMessage(infoQueueMessageStr);
            moreInfoEventQueue.AddMessage(infoQueueMessage);
           
            return eventResult;
        }

    }

    public class FacebookUser
    {
        public string id { get; set; }
        public string name { get; set; }
        public string pic { get; set; }
        public FacebookUser(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    public class FacebookCheckin
    {
        public string author_uid { get; set; }
        public string author_name { get; set; }
        public string checkin_id { get; set; }
        public string timestamp { get; set; }
        public string message { get; set; }
        public FacebookLocation location { get; set; }
        public List<string> tagged_uids { get; set; }
        public string page_id { get; set; }
    }

    public class FacebookEvent
    {
        public string name { get; set; }
        public string description { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string eid { get; set; }
        public string pic { get; set; }
        public string host { get; set; }
        public FacebookLocation venue { get; set; }
        public List<FacebookUser> attendantList { get; set; }
    }

    public class FacebookLocation
    {
        public string id { get; set; }
        public string name { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public string country { get; set; }
    }

    public class FacebookSearchResult
    {
        public string name { get; set; }
        public IList<dynamic> fql_result_set { get; set; }
    }

    public class FacebookSearchResults
    {
        public IList<FacebookSearchResult> data;
    }

    public class EventsSearchResult
    {
        public List<Place> events = new List<Place>();
    }
}
