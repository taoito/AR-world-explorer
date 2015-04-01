using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Facebook;
using Newtonsoft.Json;
using ARWebRole.Models;

namespace ARWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        static string GoogleAPIKey = "AIzaSyDN_W-Ya3sz38jT2zMRVQZLvbajQlyjbEA";
        private string Yelp_id = "fTAvB_7Kbh6YEdKiXhJjAQ";
        //static string Yelp_id2 = "QyeHrQVn8v8Up8Irh88q1w";
        //static string Yelp_id3 = "q36nDxS-IEPnFsO9ImtrxQ";
        private CloudQueue moreInfoPlaceQueue;
        private CloudQueue moreInfoEventQueue;
        private CloudTable PlacesTable;
        private CloudTable EventsTable;
        private volatile bool onStopCalled = false;
        private volatile bool returnedFromRunMethod = false;

        private void ConfigureDiagnostics()
        {
            DiagnosticMonitorConfiguration config = DiagnosticMonitor.GetDefaultInitialConfiguration();
            config.ConfigurationChangePollInterval = TimeSpan.FromMinutes(1d);
            config.Logs.BufferQuotaInMB = 500;
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
            config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1d);

            DiagnosticMonitor.Start(
                "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString",
                config);
        }

        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount;

            ConfigureDiagnostics();
            Trace.TraceInformation("Initializing storage account in AR Worker");
            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            moreInfoPlaceQueue = queueClient.GetQueueReference("moreinfoplacequeue");
            moreInfoEventQueue = queueClient.GetQueueReference("moreinfoeventqueue");
            var tableClient = storageAccount.CreateCloudTableClient();
            PlacesTable = tableClient.GetTableReference("PlacesTable");
            EventsTable = tableClient.GetTableReference("EventsTable");

            // Create if not exists for queue, blob container, place table. 
            moreInfoPlaceQueue.CreateIfNotExists();
            moreInfoEventQueue.CreateIfNotExists();
            PlacesTable.CreateIfNotExists();
            EventsTable.CreateIfNotExists();
            return base.OnStart();
        }

        public override void OnStop()
        {
            onStopCalled = true;
            while (returnedFromRunMethod == false)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        //Retrieve a Google Place entry from the Azure Table
        private GooglePlaceTable FindPlace(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<GooglePlaceTable>(partitionKey, rowKey);
            var retrievedResult = PlacesTable.Execute(retrieveOperation);
            var gPlace = retrievedResult.Result as GooglePlaceTable;
            return gPlace;
        }

        //Retrieve a Facebook Event entry from the Azure Table
        private FacebookEventTable FindEvent(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<FacebookEventTable>(partitionKey, rowKey);
            var retrievedResult = EventsTable.Execute(retrieveOperation);
            var fbEvent = retrievedResult.Result as FacebookEventTable;
            return fbEvent;
        }

        public override void Run()
        {
            Trace.TraceInformation("AR Worker Role entering Run()");

            CloudQueueMessage infoPlaceQueueMessage = null;
            CloudQueueMessage infoEventQueueMessage = null;

            while (true)
            {
                try
                {
                    //var tomorrow = DateTime.Today.AddDays(1.0).ToString("yyyy-MM-dd");
                    // If OnStop has been called, return to do a graceful shutdown.
                    if (onStopCalled == true)
                    {
                        Trace.TraceInformation("onStopCalled AR Worker Role");
                        returnedFromRunMethod = true;
                        return;
                    }

                    bool messageFound = false;

                    //Pop message from the Azure More Info Place Queue
                    infoPlaceQueueMessage = moreInfoPlaceQueue.GetMessage();
                    if (infoPlaceQueueMessage != null)
                    {
                        ProcessPlaceQueueMessage(infoPlaceQueueMessage);
                        messageFound = true;
                    }

                    //Pop message from the Azure More Info Event Queue
                    infoEventQueueMessage = moreInfoEventQueue.GetMessage();
                    if (infoEventQueueMessage != null)
                    {
                        ProcessEventQueueMessage(infoEventQueueMessage);
                        messageFound = true;
                    }

                    // Sleep for 20s to minimize query costs. 
                    if (messageFound == false)
                    {
                        System.Threading.Thread.Sleep(1000 * 20);
                    }

                    // Retrieve all places that only have basic info.
                    /*string placeFilter = TableQuery.GenerateFilterCondition("HasMoreInfo", QueryComparisons.Equal, "false");
                    var query = (new TableQuery<GooglePlaceTable>().Where(placeFilter));
                    var placesToFindDetails = placeOfInterestTable.ExecuteQuery(query).ToList();
                     */                              
                }
                catch (Exception ex)
                {
                    string err = ex.Message;
                    if (ex.InnerException != null)
                    {
                        err += " Inner Exception: " + ex.InnerException.Message;
                    }
                    Trace.TraceError(err);
                    // Don't fill up Trace storage if we have a bug in queue process loop.
                    System.Threading.Thread.Sleep(1000 * 60);
                }
            }
        }

        private void ProcessEventQueueMessage(CloudQueueMessage infoQueueMessage)
        {
            // Log and delete if this is a corrupted queue message (repeatedly processed
            // and always causes an error that prevents processing from completing).         
            if (infoQueueMessage.DequeueCount > 2)
            {
                Trace.TraceError("Deleting corrupted message: message {0} ", infoQueueMessage.ToString());
                moreInfoEventQueue.DeleteMessage(infoQueueMessage);
                return;
            }
            //Parse the queue message into a list of new Facebook Events
            string infoQueueMessageStr = infoQueueMessage.AsString.ToString();
            InfoQueueEvent infoQueueEvent = JsonConvert.DeserializeObject<InfoQueueEvent>(infoQueueMessageStr);

            if (infoQueueEvent.newFacebookEvents[0].UserId != null)
            {
                GetEventAttendants(infoQueueEvent.newFacebookEvents[0].UserId, infoQueueEvent.newFacebookEvents[0].AccessToken, infoQueueEvent);
            }
            
            /*foreach (FacebookEventTable fbEvent in infoQueueEvent.newFacebookEvents)
            {
                if (FindEvent(fbEvent.UserId, fbEvent.Id) == null)
                {
                    var insertOperation = TableOperation.Insert(fbEvent);
                    EventsTable.Execute(insertOperation);
                }
            }*/

        }

        public void GetEventAttendants(string userId, string accessToken, InfoQueueEvent infoQueueEvent)
        {
            Dictionary<string, List<FacebookUser>> eventIdToAttendants = new Dictionary<string, List<FacebookUser>>();
            Dictionary<string, FacebookUser> userIdToFacebookUser = new Dictionary<string, FacebookUser>();

            FacebookSearchResults queryResult;

            Stopwatch sw = new Stopwatch();

            try
            {
                var fb = new FacebookClient(accessToken);

                sw.Start();
                queryResult = fb.Get<FacebookSearchResults>("fql",
                                            new
                                            {
                                                q = new
                                                {
                                                    friend_ids = "SELECT uid2 FROM friend WHERE uid1 = me()",
                                                    friend_info = "SELECT id, name, pic FROM profile WHERE id IN (SELECT uid2 FROM #friend_ids)",
                                                    event_member_info = "SELECT eid, uid FROM event_member WHERE rsvp_status = 'attending' AND uid IN (SELECT uid2 from #friend_ids)",
                                                }
                                            });
                sw.Stop();
                System.Diagnostics.Debug.WriteLine("Elapsed time: {0}", sw.Elapsed);
            }
            #region Exception
            catch (FacebookOAuthException)
            {
                Console.WriteLine("oauth exception occurred");
                throw;
            }
            catch (FacebookApiLimitException)
            {
                Console.WriteLine("api limit exception occurred");
                throw;
            }
            catch (FacebookApiException)
            {
                Console.WriteLine("other general facebook api exception");
                throw;
            }
            catch (Exception)
            {
                Console.WriteLine("non-facebook exception raised");
                throw;
            }
            #endregion

            // TODO: Getting user's friends attending events
            FacebookSearchResult friend_info = new FacebookSearchResult();
            FacebookSearchResult event_member_info = new FacebookSearchResult();

            foreach (FacebookSearchResult fsr in queryResult.data)
            {
                switch (fsr.name)
                {
                    case "friend_info":
                        friend_info = fsr;
                        break;
                    case "event_member_info":
                        event_member_info = fsr;
                        break;
                    default:
                        break;
                }
            }

            IList<dynamic> friends = friend_info.fql_result_set;

            IList<dynamic> events = event_member_info.fql_result_set;

            // Getting userIdToFacebookUser dicitonary
            for (int i = 0; i < friends.Count; i++)
            {
                JsonObject currFriend = (JsonObject)friends[i];

                if ((!currFriend.ContainsKey("name")) || (!currFriend.ContainsKey("id")))
                    continue;

                FacebookUser fu = new FacebookUser(currFriend["id"].ToString(), (string)currFriend["name"]);
                fu.pic = currFriend.ContainsKey("pic") ? (string)currFriend["pic"] : "";

                userIdToFacebookUser.Add(fu.id, fu);
            }

            for (int i = 0; i < events.Count; i++)
            {
                JsonObject currEvent = (JsonObject)events[i];

                if ((!currEvent.ContainsKey("eid")) || (!currEvent.ContainsKey("uid")))
                    continue;

                string eid = currEvent["eid"].ToString();
                string uid = currEvent["uid"].ToString();

                if (!userIdToFacebookUser.ContainsKey(uid))
                    continue;

                FacebookUser facebookUser = userIdToFacebookUser[uid];

                if (!eventIdToAttendants.ContainsKey(eid))
                {
                    List<FacebookUser> newList = new List<FacebookUser>();
                    newList.Add(facebookUser);
                    eventIdToAttendants.Add(eid, newList);
                }
                else
                {
                    eventIdToAttendants[eid].Add(facebookUser);
                }

            }

            /*if (!userIdToEvents.ContainsKey(userId))
            {
                // This user id is not profiled
                return null;
            }

            Dictionary<string, FacebookEvent> userEvents = userIdToEvents[userId];
           
            foreach (string eventId in eventIdToAttendants.Keys)
            {
                if (FindEvent(userId, eventId) == null)
                {
                    userEvents[eventId].attendantList = eventIdToAttendants[eventId];
                }
            }*/

            foreach (FacebookEventTable fbEvent in infoQueueEvent.newFacebookEvents)
            {
                FacebookEventTable searchEvent = FindEvent(fbEvent.UserId, fbEvent.Id);
                List<string> attendantList = new List<string>();
                List<string> attendantPicList = new List<string>();
                foreach (FacebookUser user in eventIdToAttendants[fbEvent.Id])
                {
                    attendantList.Add(user.name);
                    attendantPicList.Add(user.pic);
                }

                if (searchEvent == null)
                {
                    fbEvent.AttendantList = JsonConvert.SerializeObject(attendantList);
                    fbEvent.AttendantPicList = JsonConvert.SerializeObject(attendantPicList);
                    fbEvent.StreetViewBlobRef = "http://maps.googleapis.com/maps/api/streetview?size=300x100&location="
                                                + fbEvent.Lat + "," + fbEvent.Lng + "&fov=120&sensor=false";
                    var insertOperation = TableOperation.Insert(fbEvent);
                    EventsTable.Execute(insertOperation);
                }
                else
                {
                    searchEvent.AttendantList = JsonConvert.SerializeObject(attendantList);
                    searchEvent.AttendantPicList = JsonConvert.SerializeObject(attendantPicList); 
                    TableOperation replaceOperation;
                    replaceOperation = TableOperation.Replace(searchEvent);
                    EventsTable.Execute(replaceOperation);
                }
            }

        }


        private void ProcessPlaceQueueMessage(CloudQueueMessage infoQueueMessage)
        {

            GooglePlaceTable meta = FindPlace("Meta", "YelpID");
            if (meta != null)
            {
                Yelp_id = meta.Name;
            }

            string[] daysOfWeek = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            // Log and delete if this is a corrupted queue message (repeatedly processed
            // and always causes an error that prevents processing from completing).         
            if (infoQueueMessage.DequeueCount > 2)
            {
                Trace.TraceError("Deleting corrupted message: message {0} ", infoQueueMessage.ToString());
                moreInfoPlaceQueue.DeleteMessage(infoQueueMessage);
                return;
            }

            //Parse the queue message into a list of new Google Places
            string infoQueueMessageStr = infoQueueMessage.AsString.ToString();
            InfoQueueItem infoQueueItem = JsonConvert.DeserializeObject<InfoQueueItem>(infoQueueMessageStr);

            /*
            GooglePlaceTable place1 = new GooglePlaceTable();
            place1.Id = "Test1";
            place1.Address = infoQueueMessageStr;
            var insertOperation = TableOperation.Insert(place1);
            placeOfInterestTable.Execute(insertOperation);

            GooglePlaceTable place2 = new GooglePlaceTable();
            place2.Id = "Test2";
            place2.Address = infoQueueItem.ToString();
            var insertOperation2 = TableOperation.Insert(place2);
            placeOfInterestTable.Execute(insertOperation2);
            

            GooglePlaceTable place3 = infoQueueItem.newGooglePlaces[0];
            //place3.Id = "Test3";
            //place3.Address = infoQueueItem.newGooglePlaces[0].Name;
            var insertOperation3 = TableOperation.Insert(place3);
            placeOfInterestTable.Execute(insertOperation3);
        */

            // Get detailed information for each place.
            foreach (GooglePlaceTable gPlace in infoQueueItem.newGooglePlaces)
            {
                if (FindPlace(gPlace.PartitionKey, gPlace.Id) == null)
                {
                    Trace.TraceInformation("Getting Google Details for {0} ", gPlace.Name);
                    //From Google Place Details
                    string url = "https://maps.googleapis.com/maps/api/place/details/json?reference=" +
                            gPlace.Reference + "&sensor=false&key=" + GoogleAPIKey;

                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = client.GetAsync(url).Result; // Blocking call!

                    if (response.IsSuccessStatusCode)
                    {
                        GoogleDetailResponse detailResponse = response.Content.ReadAsAsync<GoogleDetailResponse>().Result;
              
                        gPlace.Address = detailResponse.result.formatted_address;                                         
                        gPlace.Website = detailResponse.result.website;
                        gPlace.PhoneNumber = detailResponse.result.formatted_phone_number;     
                        if (detailResponse.result.photos != null)
                        {
                            gPlace.PhotoBlobRef = "https://maps.googleapis.com/maps/api/place/photo?maxwidth=400&photoreference="
                                + detailResponse.result.photos[0].photo_reference + "&sensor=false&key=" + GoogleAPIKey;
                        }
                        //string gOpeningHours = JsonConvert.SerializeObject(detailResponse.result.opening_hours.periods);                 
                        List<string> gOpeningHours = new List<string>();
                        if (detailResponse.result.opening_hours != null)
                        {
                            foreach (OpeningPeriod period in detailResponse.result.opening_hours.periods)
                            {
                                gOpeningHours.Add(daysOfWeek[period.open.day] + ": " + period.open.time + "-" + period.close.time);
                            }
                        }
                        gPlace.OpeningHours = JsonConvert.SerializeObject(gOpeningHours);
                        gPlace.StreetViewBlobRef = "http://maps.googleapis.com/maps/api/streetview?size=300x100&location="
                                                + gPlace.Lat + "," + gPlace.Lng + "&fov=120&sensor=false";

                    }

                    Trace.TraceInformation("Getting Yelp Reviews for {0} ", gPlace.Name);
                    //From Yelp Reviews
                    string yelpUrl = "http://api.yelp.com/business_review_search?term=" + gPlace.Name 
                                    + "&lat=" + gPlace.Lat.ToString() + "&long=" + gPlace.Lng.ToString()
                                    + "&radius=1&limit=1&ywsid=" + Yelp_id;

                    HttpClient client2 = new HttpClient();
                    client2.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage yelpResponse = client2.GetAsync(yelpUrl).Result; // Blocking call!

                    YelpReviewResponse reviewResponse = null;

                    if (yelpResponse.IsSuccessStatusCode)
                    {
                        string output = yelpResponse.Content.ReadAsStringAsync().Result;
                        reviewResponse = JsonConvert.DeserializeObject<YelpReviewResponse>(output);

                        //gPlace.Reviews = output;
                        if (reviewResponse.businesses != null && reviewResponse.businesses.Count > 0 )
                        {
                            YelpBusiness allReviews = reviewResponse.businesses[0];
                            
                            //string gReviews = JsonConvert.SerializeObject(allReviews);
                            List<string> gReviews = new List<string>();
                            if (allReviews.reviews != null)
                            {
                                foreach (YelpReview review in allReviews.reviews)
                                {
                                    gReviews.Add(review.user_name + ": [" + review.rating + "/5] " + review.text_excerpt); 
                                }
                            }
                            gPlace.Reviews = JsonConvert.SerializeObject(gReviews);
                        }
                        
                    }

                    gPlace.PublicCheckins = 0;
                    //Insert new place into Azure Table
                    var insertOperation = TableOperation.Insert(gPlace);
                    PlacesTable.Execute(insertOperation);

                    //TableOperation replaceOperation;
                    //replaceOperation = TableOperation.Replace(gPlace);
                    //placeOfInterestTable.Execute(replaceOperation);
                }
            }
        }

        public class InfoQueueItem
        {
            public List<GooglePlaceTable> newGooglePlaces = new List<GooglePlaceTable>();
        }

        public class InfoQueueEvent
        {
            public List<FacebookEventTable> newFacebookEvents = new List<FacebookEventTable>();
        }

        public class YelpReviewResponse
        {
            public List<YelpBusiness> businesses { get; set; }
        }
        public class YelpBusiness
        {
            public List<YelpReview> reviews { get; set; }
        }
        public class YelpReview
        {
            public int rating { get; set; }
            public string url { get; set; }
            public string text_excerpt { get; set; }
            public string user_name { get; set; }
        }

        public class GoogleDetailResponse
        {
            public List<string> html_attributions { get; set; }
            public GoogleDetailResult result { get; set; }
            public string status { get; set; }
        }

        public class GoogleDetailResult
        {
            public string formatted_address { get; set; }
            public string formatted_phone_number { get; set; }
            public string website { get; set; }
            //public List<Review> reviews { get; set; }
            public List<Photo> photos { get; set; }
            public OpeningHours opening_hours { get; set; }
        }

        public class Review
        {
            public string text { get; set; }
        }

        public class Photo
        {
            public string photo_reference { get; set; }
        }

        public class OpeningHours
        {
            public List<OpeningPeriod> periods { get; set; }
        }
        public class OpeningPeriod
        {
            public ServiceTime close { get; set; }
            public ServiceTime open { get; set; }
        }
        public class ServiceTime
        {
            public int day { get; set; }
            public string time { get; set; }
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
       
    }
}
