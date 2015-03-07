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
        // For location only, testing
        public static string CreateLocationRequest(double latitude, double longitude, double radius, string type)
        {
            string urlRequest = "http://arworld.azurewebsites.net/api/"
                                    + "locations/list?"
                                    + "lat=" + latitude.ToString()
                                    + "&lon=" + longitude.ToString()
                                    + "&radius=" + radius.ToString()
                                    + "&type=" + type;

            return urlRequest;
        }

        public static string CreateEventRequest(double latitude, double longitude, double radius, string userId, string accessToken)
        {
            string urlRequest = "http://arworld.azurewebsites.net/api/"
                                    + "events/list?"
                                    + "lat=" + latitude.ToString()
                                    + "&lon=" + longitude.ToString()
                                    + "&radius=" + radius.ToString()
                                    + "&userId=" + userId
                                    + "&accessToken=" + accessToken;

            return urlRequest;
        }

        public static string CreateYelpRequest(double latitude, double longtitude, double radius, string id)
        {
            string urlRequest = "http://api.yelp.com/"
                                   + "business_review_search?term=bars"
                                   + "&lat=" + latitude.ToString()
                                   + "&long=" + longtitude.ToString()
                                   + "&radius=" + radius.ToString()
                                   + "&ywsid=" + id.ToString();

            return urlRequest;


        }
    }
}
