using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using WorldExplorerServer.Models;
using Newtonsoft.Json;
using System.IO;

namespace WorldExplorerServer.Controllers
{
    public class LocationsController : ApiController
    {
        static string GoogleAPIKey = "AIzaSyDN_W-Ya3sz38jT2zMRVQZLvbajQlyjbEA";
        static string placeSearch = "placeSearch";
        static string placeDetails = "placeDetails";
        static string yelp = "yelp";
        static string Yelp_id = "QyeHrQVn8v8Up8Irh88q1w";

        // Sample request below:
        // https://maps.googleapis.com/maps/api/place/nearbysearch/json?location=-33.8670522,151.1957362&radius=500&types=food&name=harbour&sensor=false&key=AIzaSyDN_W-Ya3sz38jT2zMRVQZLvbajQlyjbEA

        // Place Search Request
        public Response GetList(double lat, double lon, int radius, string type)
        {
            string url;

            
            if (type != null && type.Equals("food"))
            {
                // Yelp Place Search
                url = "http://api.yelp.com/business_review_search?term=yelp" + 
                        "&lat=" + lat.ToString() + "&long=" + lon.ToString() + 
                        "&radius=" + radius.ToString() + "&limit=10" + "&ywsid" + Yelp_id;

                return SendRequest(url, yelp);
            }
            else
            {
                // Google Place Search
                url = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?" +
                         "location=" + lat.ToString() + "," + lon.ToString() +
                         "&radius=" + radius.ToString() + "&types=" + type +
                         "&sensor=false&key=" + GoogleAPIKey;

                return SendRequest(url, placeSearch);
            }

        }

        // Google Place Details Request
        public Response GetPlaceDetails(string id)
        {
            string url = "https://maps.googleapis.com/maps/api/place/details/json?" +
                         "reference=" + id + "&sensor=false&key=" + GoogleAPIKey;

            return SendRequest(url, placeDetails);

        }

        // Creates http request, reads response asynchronously, deserializes response
        private Response SendRequest(string url, string type)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage httpResponse = client.GetAsync(url).Result; // Blocking call!
            PlaceInfo deserialized = null;
            Response response = null;

            try
            {
                if (httpResponse.IsSuccessStatusCode)
                {
                    string output = httpResponse.Content.ReadAsStringAsync().Result;

                    // For Basic Requests
                    if (type.Equals(placeSearch))
                    {
                        deserialized = JsonConvert.DeserializeObject<PlaceSearch>(output);
                        response = new Basic();
                        ((Basic)response).locations = new List<Place>();

                        foreach (Result place in ((PlaceSearch)deserialized).results)
                        {
                            ((Basic)response).locations.Add(new Place(place.reference, place.geometry.location, place.name));
                        }
                    }

                    // For Detailed Requests
                    else if (type.Equals(placeDetails))
                    {
                        deserialized = JsonConvert.DeserializeObject<PlaceDetails>(output);
                        Details place_details = ((PlaceDetails)deserialized).result;
                        response = new Detailed();
                        ((Detailed)response).id = place_details.reference;
                        ((Detailed)response).name = place_details.name;
                        ((Detailed)response).location = place_details.geometry.location;
                        ((Detailed)response).types = place_details.types;
                        ((Detailed)response).address = place_details.formatted_address;
                        ((Detailed)response).phone_number = place_details.formatted_phone_number;
                        ((Detailed)response).website = place_details.website;
                        ((Detailed)response).rating = place_details.rating;
                        ((Detailed)response).opening_hours = place_details.opening_hours;
                        ((Detailed)response).reviews = place_details.reviews;
                        ((Detailed)response).events = place_details.events;
                        ((Detailed)response).streetview = ImageGrab(place_details.geometry.location);
                    }

                    // For Yelp Requests
                    else if (type.Equals(yelp))
                    {
                        deserialized = JsonConvert.DeserializeObject<YelpSearch>(output);
                    }

                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)httpResponse.StatusCode, httpResponse.ReasonPhrase);
                    throw new HttpResponseException(
                        Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                        "Could not contact Google Places API")
                    );

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            return response;

        }

        // grabs byte array of Google StreetView image then converted to a base64String to be passed into JSON response
        // NOTE base64String of the image must be converted to the actual image on the phone side:
        /* Convert Base64 String to byte[]
         * byte[] imageBytes = Convert.FromBase64String(base64String);
         * MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
         *  
         * Convert byte[] to Image
         * ms.Write(imageBytes, 0, imageBytes.Length);
         * Image image = Image.FromStream(ms, true);
         * return image;
         */
        private string ImageGrab(Location loc)
        {
            string url = "http://maps.googleapis.com/maps/api/streetview?size=150x150" +
                         "&location=" + loc.lat.ToString() + "," + loc.lng.ToString() +
                         "&sensor=false&key=" + GoogleAPIKey;

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
            HttpResponseMessage httpResponse = client.GetAsync(url).Result;
            Byte[] barray = null;

            try
            {
                if (httpResponse.IsSuccessStatusCode)
                {
                    barray = httpResponse.Content.ReadAsByteArrayAsync().Result;
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)httpResponse.StatusCode, httpResponse.ReasonPhrase);
                    throw new HttpResponseException(
                        Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                        "Could not contact Google Places API")
                    );

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            return Convert.ToBase64String(barray);
        }

    }

    //******************************//
    // Actual objects sent to phone //
    //******************************//

    public class Response { }

    public class Basic : Response 
    {
        public List<Place> locations { get; set; }
    }

    public class Detailed : Response
    {
        public string id { get; set; }
        public string name { get; set; }
        public Location location { get; set; }
        public List<string> types { get; set; }
        public string address { get; set; }
        public string phone_number { get; set; }
        public string website { get; set; }
        public double rating { get; set; }
        public OpeningHours opening_hours { get; set; }
        public List<Review> reviews { get; set; }
        public List<Event> events { get; set; }
        public string streetview { get; set; }
    }

    //**************************************************//
    // Initial JSON deserialization of search responses //
    //**************************************************//

    public class PlaceInfo { }

    public class PlaceSearch : PlaceInfo
    {
        public List<Result> results { get; set; }
        public List<string> html_attributions { get; set; }
        public string status { get; set; }
    }

    public class PlaceDetails : PlaceInfo
    {
        public Details result { get; set; }
        public List<string> html_attributions { get; set; }
        public string status { get; set; }
    }

    public class YelpSearch : PlaceInfo
    {
        public List<Business> businesses;
        public Message message;
    }

    public class Geometry
    {
        public Location location { get; set; }
    }

    public class Result
    {
        public string formatted_address { get; set; }
        public Geometry geometry { get; set; }
        public string icon { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public double rating { get; set; }
        public string reference { get; set; }
        public List<string> types { get; set; }
        public List<Event> events { get; set; }
        public string vicinity { get; set; }
    }

    public class Event
    {
        public string event_id { get; set; }
        public long start_time { get; set; }
        public string summary { get; set; }
        public string url { get; set; }
    }

    public class Details
    {
        public string vicinity { get; set; }
        public Geometry geometry { get; set; }
        public string icon { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public double rating { get; set; }
        public string reference { get; set; }
        public List<string> types { get; set; }
        public string formatted_address { get; set; }
        public string formatted_phone_number { get; set; }
        public string international_phone_number { get; set; }
        public List<Event> events { get; set; }
        public List<Review> reviews { get; set; }
        public string url { get; set; }
        public string website { get; set; }
        public OpeningHours opening_hours { get; set; }
    }

    public class OpeningHours
    {
        public Boolean open_now { get; set; }
    }

    public class Review
    {
        // Place Details only
        public List<Aspect> aspects { get; set; }
        public string author_name { get; set; }
        public string author_url { get; set; }
        public string text { get; set; }
        public long time { get; set; }

        // Yelp only
        public string id;
        public int rating;
        public string rating_img_url;
        public string rating_img_url_small;
        public string text_excerpt;
        public string url;
        public string user_name;
        public string user_photo_url;
        public string user_photo_url_small;
        public string mobile_uri;
        public string user_url;
    }

    public class Aspect
    {
        public double rating { get; set; }
        public string type { get; set; }
    }

    public class Category
    {
        public string category_filter;
        public string name;
        public string search_url;
    }

    public class Neighborhood
    {
        public string name;
        public string url;
    }

    public class Business
    {
        public string address1;
        public string address2;
        public string address3;
        public double avg_rating;
        public List<Category> categories;
        public string city;
        public double distance;
        public string id;
        public bool is_closed;
        public double latitude;
        public double longitude;
        public string mobile_url;
        public string name;
        public string nearby_url;
        public List<Neighborhood> neighborhoods;
        public string phone;
        public string photo_url;
        public string photo_url_small;
        public string rating_img_url;
        public string rating_img_url_small;
        public int review_count;
        public List<Review> reviews;
        public string state;
        public string state_code;
        public string country;
        public string country_code;
        public string url;
        public string zip;
    }

    public class Message
    {
        public int code;
        public string text;
        public string version;
    }

}