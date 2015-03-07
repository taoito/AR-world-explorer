using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorldExplorerServer.Utilities
{
    public class Communication
    {
        private Communication()
        {

        }

        static public string createFacebookSearchRequest(double lat, double lon, double radius, string accessToken, string query = ".*")
        {
            string urlRequest = "https://graph.facebook.com/search?fields=id,name" +
                    "&q=" + query +
                    "&type=event" +
                    "&center=" + lat.ToString() +
                    "," + lon.ToString() +
                    "&distance=" + radius.ToString() +
                    "&access_token=" + accessToken;

            return urlRequest;
        }

        static public string createBatchFacebookEventsRequest()
        {
            return null;
        }

        static public string createBatchFacebookLocationsRequest()
        {
            return null;
        }

        static public string createFacebookEventRequest(string eventId, string accessToken)
        {
            string urlRequest = "https://graph.facebook.com/" + eventId + "?" +
                    "fields=name,venue" +
                    "&access_token=" + accessToken;

            return urlRequest;
        }

        static public string createFacebookLocationRequest(string locationId, string accessToken)
        {
            string urlRequest = "https://graph.facebook.com/" + locationId + "?" +
                    "fields=location" +
                    "&access_token=" + accessToken;

            return urlRequest;
        }

        static public string createFacebookFriendListRequest(string userId, string accessToken)
        {
            return "https://graph.facebook.com/" + userId + "/friends?" + 
                    "access_token=" + accessToken;
        }

        static public string createFacebookGetUserEventRequest(string userId, string accessToken)
        {
            return "https://graph.facebook.com/" + userId + "/events?" +
                    "access_token=" + accessToken;
        }

        static public string createFacebookCheckinRequest(string userId, string accessToken)
        {
            return "https://graph.facebook.com/" + userId + "/checkins?" +
                    "access_token=" + accessToken;
        }
    }
}