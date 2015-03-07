using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using WorldExplorerServer.Models;
using WorldExplorerServer.Utilities;
using Facebook;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WorldExplorerServer.Controllers
{
    public class EventsController : ApiController
    {
        Dictionary<string, Dictionary<string, FacebookEvent>> userIdToEvents = 
            new Dictionary<string,Dictionary<string,FacebookEvent>>();
        
        public Object GetList(double lat, double lon, int radius, string userId, string accessToken)
        {
            Dictionary<string, FacebookLocation> locationDictionary = new Dictionary<string, FacebookLocation>();

            Dictionary<string, Place> placeDictionary = new Dictionary<string, Place>();

            Dictionary<string, FacebookEvent> eventDictionary = new Dictionary<string, FacebookEvent>();

            EventsSearchResult eventResult = new EventsSearchResult();
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
                                                    event_info = "SELECT name, description, start_time, end_time, eid, venue, location FROM event WHERE eid IN (SELECT eid FROM event WHERE eid IN (SELECT eid FROM event_member WHERE uid IN ( SELECT uid2 FROM friend WHERE uid1 = me())))",
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

            foreach (string eventId in eventDictionary.Keys)
            {
                FacebookEvent fe = eventDictionary[eventId];
                if (Distance.getDistance(lat, lon, fe.venue.latitude,
                                            fe.venue.longitude) < radius)
                    eventResult.places.Add(new Place(fe.eid, new Location(fe.venue.latitude, fe.venue.longitude), fe.name));
            }

            sw2.Stop();
            System.Diagnostics.Debug.WriteLine("Elapsed time: {0}", sw2.Elapsed);

            // Add this new event dictionary to this userId.
            if (userIdToEvents.ContainsKey(userId))
            {
                // We already have information of this user id
                userIdToEvents[userId] = eventDictionary;
            }
            else
            {
                // This is a new user
                userIdToEvents.Add(userId, eventDictionary);
            }

            return eventResult;
        }

        #region deprecated code
        private void getUserFriends(List<FacebookUser> userFriends, string userId, string accessToken)
        {

            string facebookFriendListUrl = Communication.createFacebookFriendListRequest(userId, accessToken);
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage facebookFriendListResponse = client.GetAsync(facebookFriendListUrl).Result; // Blocking call!

            while (true)
            {
                if (facebookFriendListResponse.IsSuccessStatusCode)
                {
                    // Gonna parse result and make more http requests
                    Friendlist userFriendList =
                        facebookFriendListResponse.Content.ReadAsAsync<Friendlist>().Result;

                    foreach (FacebookUser friend in userFriendList.data)
                    {
                        userFriends.Add(friend);
                    }

                    if ( (userFriendList.paging == null) || (userFriendList.paging.next == null) )
                        break;

                    facebookFriendListUrl = userFriendList.paging.next;

                    facebookFriendListResponse = client.GetAsync(facebookFriendListUrl).Result;
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)facebookFriendListResponse.StatusCode, facebookFriendListResponse.ReasonPhrase);
                }
            }
        }

        private void getEventLocation(Dictionary<string, Place> eventDict, string eventId, string accessToken)
        {
            if(eventDict.ContainsKey(eventId))
                return;
            string getEventUrl;
            HttpResponseMessage getEventResponse;
            double lat, lon;

            getEventUrl = Communication.createFacebookEventRequest(eventId, accessToken);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            getEventResponse = client.GetAsync(getEventUrl).Result;

            if (getEventResponse.IsSuccessStatusCode)
            {
                // Gonna parse result and make more http requests
                FacebookEventData facebookEvent =
                    getEventResponse.Content.ReadAsAsync<FacebookEventData>().Result;

                if (facebookEvent.venue != null)
                {
                    lat = facebookEvent.venue.latitude;
                    lon = facebookEvent.venue.longitude;
                    string id = facebookEvent.venue.id;

                    if( (lat != 0) || (lon != 0) )
                       eventDict.Add(eventId, new Place(eventId, new Location(lat, lon), facebookEvent.name));
                    else if (id != null)
                    {
                        // Get event's location by its location address
                        string getFacebookLocationUrl = 
                            Communication.createFacebookLocationRequest(id, accessToken);
                        HttpResponseMessage getFacebookLocationResponse = 
                            client.GetAsync(getFacebookLocationUrl).Result;
                        if (getFacebookLocationResponse.IsSuccessStatusCode)
                        {
                            FacebookEventLocation eventLocation = 
                                getFacebookLocationResponse.Content.ReadAsAsync<FacebookEventLocation>().Result;
                            lat = eventLocation.location.latitude;
                            lon = eventLocation.location.longitude;
                            if ((lat != 0) || (lon != 0))
                                eventDict.Add(eventId, new Place(eventId, new Location(lat, lon), facebookEvent.name));
                        }
                        else
                        {
                            //skip it
                        }
                    }
                }
                
            }
            else
            {
                Console.WriteLine("{0} ({1})", (int)getEventResponse.StatusCode, getEventResponse.ReasonPhrase);
            }
        }


        // TODO: handle oath exception when token expires
        // TODO: display correct error message, for now it will return empty data
        public EventsSearchResult getFacebook(string accessToken)
        {
            Dictionary<string, FacebookLocation> locationDictionary = new Dictionary<string, FacebookLocation>();

            Dictionary<string, Place> placeDictionary = new Dictionary<string, Place>();

            Dictionary<string, FacebookEvent> eventDictionary = new Dictionary<string, FacebookEvent>();

            EventsSearchResult eventResult = new EventsSearchResult();
            FacebookSearchResults result;

            try
            {
                var fb = new FacebookClient(accessToken);
                result = fb.Get<FacebookSearchResults>("fql",
                    //dynamic result = fb.Get("fql",
                                            new
                                            {
                                                q = new
                                                {
                                                    event_info = "SELECT name, description, start_time, end_time, eid, venue, location FROM event WHERE eid IN (SELECT eid FROM event WHERE eid IN (SELECT eid FROM event_member WHERE uid IN ( SELECT uid2 FROM friend WHERE uid1 = me())))",
                                                    event_venue = "SELECT name, username, page_id, location  FROM page WHERE page_id IN  (SELECT venue.id FROM #event_info)",
                                                }
                                            });
            }
            catch (FacebookOAuthException)
            {
                Console.WriteLine("oauth exception occurred");
                throw;
                //return eventResult;
            }
            catch (FacebookApiLimitException)
            {
                Console.WriteLine("api limit exception occurred");
                throw;
                //return eventResult;
            }
            catch (FacebookApiException)
            {
                Console.WriteLine("other general facebook api exception");
                throw;
                //return eventResult;
            }
            catch (Exception)
            {
                Console.WriteLine("non-facebook exception raised");
                throw;
                //return eventResult;
            }

            FacebookSearchResult event_info = result.data[0];
            FacebookSearchResult event_venue = result.data[1];
            //event_info = result.data[0];

            IList<dynamic> events = event_info.fql_result_set;

            IList<dynamic> venues = event_venue.fql_result_set;

            // Parsing the location pages that the events can be at
            for (int i = 0; i < venues.Count; i++)
            {
                // Only get locations with latitude and longitude

                JsonObject venue = (JsonObject) venues[i];

                if( (!venue.ContainsKey("page_id")) || (!venue.ContainsKey("location")))
                    continue;

                JsonObject venueLocation = (JsonObject)venue["location"];
                
                if ( (!venueLocation.ContainsKey("longitude")) || (!venueLocation.ContainsKey("longitude")) )
                    continue;

                FacebookLocation el = new FacebookLocation();
                el.id = venue["page_id"].ToString() ;
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
                fe.name = (string) currEvent["name"];
                fe.eid = events[i].eid.ToString();
                fe.start_time = currEvent.ContainsKey("start_time") ? (string) currEvent["start_time"] : "";
                fe.end_time = currEvent.ContainsKey("end_time") ? (string)currEvent["end_time"] : "";
                fe.description = currEvent.ContainsKey("description") ? (string)currEvent["description"] : "";

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

            foreach (string eventId in eventDictionary.Keys)
            {
                double lat = 44.96931;
                double lon = -93.223226;
                double radius = 100;
                FacebookEvent fe = eventDictionary[eventId];
                if (Distance.getDistance(lat, lon, fe.venue.latitude, 
                                            fe.venue.longitude) < radius)
                    eventResult.places.Add(new Place(fe.eid, new Location(fe.venue.latitude, fe.venue.longitude), fe.name));
            }

            return eventResult;
        }

        #endregion

        public Object getCheckins(string accessToken)
        {
            Dictionary<string, FacebookLocation> locationDictionary = new Dictionary<string, FacebookLocation>();

            Dictionary<string, FacebookCheckin> checkinDictionary = new Dictionary<string, FacebookCheckin>();

            Dictionary<string, FacebookUser> authorDictionary = new Dictionary<string, FacebookUser>();

            EventsSearchResult checkinResult = new EventsSearchResult();

            FacebookSearchResults result;
            try
            {
                var fb = new FacebookClient(accessToken);
                result = fb.Get<FacebookSearchResults>("fql",
                                            
                                            new
                                            {
                                                q = new
                                                {
                                                    checkin_info = "SELECT checkin_id, author_uid, timestamp, message, coords, tagged_uids, page_id FROM checkin WHERE author_uid IN ( SELECT uid2 FROM friend WHERE uid1 = me())",
                                                    checkin_locations = "SELECT name, username, page_id, location FROM page WHERE page_id IN  (SELECT page_id FROM #checkin_info)",
                                                    checkin_authors = "SELECT name, uid FROM user WHERE uid IN (SELECT author_uid FROM #checkin_info)",
                                                }
                                            });
                                            

            }
            #region exception stuff

            catch (FacebookOAuthException)
            {
                Console.WriteLine("oauth exception occurred");
                return new { error = "OAuthException" };
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
            #endregion


            FacebookSearchResult checkin_info = new FacebookSearchResult();
            FacebookSearchResult checkin_venues = new FacebookSearchResult();
            FacebookSearchResult checkin_authors = new FacebookSearchResult();

            foreach (FacebookSearchResult fsr in result.data)
            {
                switch (fsr.name)
                {
                    case "checkin_info":
                        checkin_info = fsr;
                        break;
                    case "checkin_locations":
                        checkin_venues = fsr;
                        break;
                    case "checkin_authors":
                        checkin_authors = fsr;
                        break;
                    default:
                        break;
                }
            }

            //event_info = result.data[0];

            IList<dynamic> checkins = checkin_info.fql_result_set;

            IList<dynamic> venues = checkin_venues.fql_result_set;

            IList<dynamic> authors = checkin_authors.fql_result_set;

            #region Getting the locations of the checkins
            for (int i = 0; i < venues.Count; i++)
            {
                // Only get locations with latitude and longitude

                JsonObject venue = (JsonObject)venues[i];

                if ((!venue.ContainsKey("page_id")) || (!venue.ContainsKey("location")))
                    continue;

                JsonObject venueLocation = (JsonObject)venue["location"];

                if ((!venueLocation.ContainsKey("longitude")) || (!venueLocation.ContainsKey("longitude")))
                    continue;

                FacebookLocation el = new FacebookLocation();
                el.id = venue["page_id"].ToString();
                el.name = venue.ContainsKey("name") ? (string)venue["name"] : "";
                el.latitude = Convert.ToDouble(venueLocation["latitude"]);
                el.longitude = Convert.ToDouble(venueLocation["longitude"]);
                el.country = venueLocation.ContainsKey("country") ? (string)venueLocation["country"] : "";
                el.state = venueLocation.ContainsKey("state") ? (string)venueLocation["state"] : "";
                el.street = venueLocation.ContainsKey("street") ? (string)venueLocation["street"] : "";
                el.zip = venueLocation.ContainsKey("zip") ? (string)venueLocation["zip"] : "";

                locationDictionary.Add(venue["page_id"].ToString(), el);
            }
            #endregion

            #region Getting checkin's author's information
            for (int i = 0; i < authors.Count; i++)
            {
                // Only get locations with latitude and longitude

                JsonObject authorJson = (JsonObject)authors[i];

                if ((!authorJson.ContainsKey("uid")) || (!authorJson.ContainsKey("name")))
                    continue;
                FacebookUser fu = new FacebookUser(authorJson["uid"].ToString(), (string)authorJson["name"]);

                authorDictionary.Add(fu.id, fu);
            }
            #endregion

            #region Getting check in information
            for (int i = 0; i < checkins.Count; i++)
            {
                JsonObject currCheckin = (JsonObject)checkins[i];

                if ((!currCheckin.ContainsKey("author_uid")) || (!currCheckin.ContainsKey("page_id")))
                    continue;

                FacebookCheckin fc = new FacebookCheckin();
                fc.message = currCheckin.ContainsKey("message") ? (string)currCheckin["message"] : "";
                fc.checkin_id = currCheckin.ContainsKey("checkin_id") ? currCheckin["checkin_id"].ToString() : "";
                fc.timestamp = currCheckin.ContainsKey("timestamp") ? currCheckin["timestamp"].ToString() : "";
                fc.author_uid = currCheckin["author_uid"].ToString();
                fc.page_id = currCheckin["page_id"].ToString();

                // Get author name from author dictionary
                if (authorDictionary.ContainsKey(fc.author_uid))
                {
                    fc.author_name = authorDictionary[fc.author_uid].name;
                }
                else
                {
                    continue;
                }

                //Get location from location dictionary
                if (locationDictionary.ContainsKey(fc.page_id))
                {
                    fc.location = locationDictionary[fc.page_id];
                }
                else
                {
                    continue;
                }

                checkinDictionary.Add(fc.checkin_id, fc);
            }
            #endregion

            #region Getting checkins in range
            foreach (string checkinId in checkinDictionary.Keys)
            {
                double lat = 44.96931;
                double lon = -93.223226;
                double radius = 10;
                FacebookCheckin fc = checkinDictionary[checkinId];
                if (Distance.getDistance(lat, lon, fc.location.latitude,
                                            fc.location.longitude) < radius)
                    checkinResult.places.Add(new Place(fc.checkin_id,
                                                        new Location(fc.location.latitude, fc.location.longitude),
                                                        fc.author_name + " is at " + fc.location.name + ": " + fc.message));
            }
            #endregion

            return checkinResult;
        }

        // What we want to get from this function is eventIdToAttendants dictionary
        //public Dictionary<string, List<FacebookUser> > getEventAttendants(string accessToken)
        public Object GetEventAttendants(string userId, string accessToken)
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
                
                if(!userIdToFacebookUser.ContainsKey(uid))
                    continue;

                FacebookUser facebookUser = userIdToFacebookUser[uid];

                if(!eventIdToAttendants.ContainsKey(eid)) {
                    List<FacebookUser> newList = new List<FacebookUser>();
                    newList.Add(facebookUser);
                    eventIdToAttendants.Add(eid, newList);
                }
                else {
                    eventIdToAttendants[eid].Add(facebookUser);
                }
                
            }

            if (!userIdToEvents.ContainsKey(userId))
            {
                // This user id is not profiled
                return null;
            }

            Dictionary<string, FacebookEvent> userEvents = userIdToEvents[userId];

            foreach (string eventId in eventIdToAttendants.Keys)
            {
                if(userEvents.ContainsKey(eventId))
                    userEvents[eventId].attendantList = eventIdToAttendants[eventId];
            }

            return null;
        }

        public Object GetDetails(string userId, string eventId)
        {
            if (userIdToEvents.ContainsKey(userId))
                return userIdToEvents[userId];
            else
                return new { error = "NotExistedUserId" };
        }
    }

    #region Data structures
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

    public class EventsSearchResult
    {
        public List<Place> places = new List<Place>();
    }
    #endregion

}
