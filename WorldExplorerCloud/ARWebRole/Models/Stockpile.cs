using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using ARWebRole.Controllers;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing;

namespace ARWebRole.Models
{
        // class for Caching
        public class Stockpile
        {
            private static Dictionary<Location, Stock> genInfo;
            private LocationComparer lc;
            private PlaceComparer pc;
            private static double MToDegConvConst = 180.0 / (Math.PI * 6356800.0);
            private static Stopwatch stopwatch = null;
            private static int tableCount;
            private const int ELEMENT_THRESHOLD = 500;
            private const int TIME_THRESHOLD = 300000;

            public Stockpile() 
            {
                lc = new LocationComparer();
                pc = new PlaceComparer();
                genInfo = new Dictionary<Location, Stock>(lc);
                if (stopwatch == null)
                {
                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                }
                tableCount = 0;
            }

            private class Stock
            {
                public Place place { get; set; }
                public String type { get; set; }
                public long timestamp { get; set; }

                public Stock() { }

                public Stock(Place place, String type, long time)
                {
                    this.place = place;
                    this.type = type;
                    this.timestamp = timestamp;
                }            
            }

            // for comparing equality between Location objects
            private class LocationComparer : IEqualityComparer<Location>
            {
                public bool Equals(Location x, Location y)
                {
                    return x.lat == y.lat && x.lng == y.lng;
                }

                public int GetHashCode(Location loc)
                {
                    Location newLoc = new Location(Math.Truncate(loc.lat), Math.Truncate(loc.lng));
                    return newLoc.ToString().GetHashCode();
                }
            }

            // for Place object comparisons
            private class PlaceComparer : IComparer<Place>
            {
                public int Compare(Place x, Place y)
                {
                    // for now assuming x & y are not null
                    if (x.location.lat > y.location.lat) return 1;
                    else if (x.location.lat == y.location.lat)
                    {
                        if (x.location.lng > y.location.lng) return 1;
                        else if (x.location.lng < y.location.lng) return -1;
                        else return 0;
                    }
                    else return -1;
                }
            }

            public Location idToLocation(string ID)
            {
                Location returnValue = null;
                IEnumerable<KeyValuePair<Location, Stock>> coordinates;
                {
                    coordinates = genInfo.Where(loc => (loc.Value.place.id.Equals(ID)));
                }

                foreach (KeyValuePair<Location, Stock> kvp in coordinates)
                {
                    returnValue = kvp.Key;
                    break;
                }

                return returnValue; 
            }

            // for saving return values of 'GetList' requests
            public void saveResult(AzureSearchResult result, string type)
            {
                Stock stock;

                // adds AzureSearchResults to hashtable
                foreach (Place place in result.places)
                {
                    if (result.places.Count + tableCount > ELEMENT_THRESHOLD)
                    {
                        removeFromTable(stopwatch.ElapsedMilliseconds);
                    }

                    if (!genInfo.ContainsKey(place.location))
                    {
                        stock = new Stock(place, type, stopwatch.ElapsedMilliseconds);
                        genInfo.Add(place.location, stock);
                        tableCount++;
                    }

                }

            }

            // removes all elements in hashtable that are >= TIME_THRESHOLD
            private void removeFromTable(long now)
            {
                List<Location> toBeRemoved = new List<Location>();
                foreach (KeyValuePair<Location, Stock> kvp in genInfo)
                {
                    if ((now - kvp.Value.timestamp) >= TIME_THRESHOLD)
                    {
                        toBeRemoved.Add(kvp.Key);
                        tableCount--;
                    }
                }

                foreach (Location key in toBeRemoved)
                {
                    genInfo.Remove(key);
                }
            }

            // for retrieving place info in the range (lat - rad, lng - rad) to (lat + rad, lng + rad)
            // where rad is converted from meter to degree
            public AzureSearchResult getPlaceInfo(double lat, double lng, double rad, string type)
            {
                AzureSearchResult result = new AzureSearchResult();

                double radDeg = MToDegConvConst * rad;
                double latMin = lat - radDeg;
                double latMax = lat + radDeg;
                double lngMin = lng - radDeg;
                double lngMax = lng + radDeg;

                // filters places with relevant coordinates
                IEnumerable<KeyValuePair<Location, Stock>> coordinates;
                if (type == null)
                {
                    coordinates = genInfo.Where(latLng =>
                        (latLng.Key.lat <= latMax &&
                        latLng.Key.lat >= latMin &&
                        latLng.Key.lng <= lngMax &&
                        latLng.Key.lng >= lngMin));
                }
                else
                {
                    coordinates = genInfo.Where(latLng =>
                        (latLng.Key.lat <= latMax &&
                        latLng.Key.lat >= latMin &&
                        latLng.Key.lng <= lngMax &&
                        latLng.Key.lng >= lngMin &&
                        latLng.Value.type.Contains(type)));
                }

                // add filtered places to AzureSearchResult place list
                foreach (KeyValuePair<Location,Stock> kvp in coordinates)
                {
                    result.places.Add(kvp.Value.place);
                }

                /* Test code for ImageGrab
                Location testLoc = new Location(44.973578, -93.228135);
                Place testPlace = new Place("TESTIMAGE", testLoc, "TESTIMAGE");
                testPlace.image = stringToImg(ImageGrab(testLoc.lat, testLoc.lng));
                result.places.Add(testPlace);
                */

                if (result.places.Count == 0)
                {
                    result = null;
                }

                return result;
            }

        }
}