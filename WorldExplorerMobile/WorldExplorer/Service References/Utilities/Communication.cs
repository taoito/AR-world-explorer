using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WorldExplorer.Utilities
{
    public class Communication
    {
        public static string CreateLocationRequest(double latitude, double longitude, double radius, string type)
        {
            string urlRequest = "http://arworldcloud.cloudapp.net/api/" //"http://arworld.azurewebsites.net/api/"
                                    + "places/basic?"
                                    + "lat=" + latitude.ToString()
                                    + "&lon=" + longitude.ToString()
                                    + "&radius=" + radius.ToString()
                                    + "&type=" + type;

            return urlRequest;
        }

        public static string CreateEventRequest(double latitude, double longitude, double radius, string userId, string accessToken)
        {
            string urlRequest = "http://arworldcloud.cloudapp.net/api/" //"http://arworld.azurewebsites.net/api/"
                                    + "events/basic?"
                                    + "lat=" + latitude.ToString()
                                    + "&lon=" + longitude.ToString()
                                    + "&radius=" + radius.ToString()
                                    + "&userId=" + userId
                                    + "&accessToken=" + accessToken;

            return urlRequest;
        }

        public static string CreateMoreDetailsRequest(string id)
        {
            string urlRequest = "http://arworldcloud.cloudapp.net/api/"
                                + "places/details?"
                                + "id=" + id;

            return urlRequest;
        }

        public static string CreateMoreDetailsEventsRequest(string eventId, string userId)
        {
            string urlRequest = "http://arworldcloud.cloudapp.net/api/"
                                + "events/details?"
                                + "eventId=" + eventId
                                +"&userId=" + userId;

            return urlRequest;
        }

    }
}
