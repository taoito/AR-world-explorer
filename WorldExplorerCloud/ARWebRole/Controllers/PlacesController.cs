using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using ARWebRole.Models;
using System.Drawing;
using System.IO;

namespace ARWebRole.Controllers
{
    public class PlacesController : ApiController
    {
        static string GoogleAPIKey = "AIzaSyDN_W-Ya3sz38jT2zMRVQZLvbajQlyjbEA";

        private CloudTable PlacesTable;
        private CloudQueue moreInfoPlaceQueue;
        private Stockpile sp;
        private CloudBlobContainer streetViewBlobContainer;

        public PlacesController()
        {
            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();
            PlacesTable = tableClient.GetTableReference("PlacesTable");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            moreInfoPlaceQueue = queueClient.GetQueueReference("moreinfoplacequeue");
            sp = MvcApplication.sp;

            // Azure Blob Stuff
            var blobClient = storageAccount.CreateCloudBlobClient();
            streetViewBlobContainer = blobClient.GetContainerReference("streetviewblobcontainer");
        }

        //Retrieve a Google Place entry from the Azure Table
        private GooglePlaceTable FindPlace(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<GooglePlaceTable>(partitionKey, rowKey);
            var retrievedResult = PlacesTable.Execute(retrieveOperation);
            var gPlace = retrievedResult.Result as GooglePlaceTable;
            return gPlace;
        }

        //Changing Yelp API in case out of quota
        public string GetYelpApi(string mode, string YelpID)
        {
            GooglePlaceTable searchMeta = FindPlace("Meta", "YelpID");
            if (searchMeta == null)
            {
                GooglePlaceTable meta = new GooglePlaceTable();
                meta.PartitionKey = "Meta";
                meta.RowKey = "YelpID";
                meta.Name = YelpID;
                var insertOperation = TableOperation.Insert(meta);
                PlacesTable.Execute(insertOperation);
            }
            else
            {
                searchMeta.Name = YelpID;
                TableOperation replaceOperation;
                replaceOperation = TableOperation.Replace(searchMeta);
                PlacesTable.Execute(replaceOperation);
            }
            return YelpID;
        }

        public AzureDetailResult GetDetails(string id)
        {
            AzureDetailResult azureDetailResult = new AzureDetailResult();

            GooglePlaceTable detailPlace = new GooglePlaceTable();
            detailPlace = FindPlace("googleplace", id);

            GooglePlaceMobileResponse detailMobileResponse = new GooglePlaceMobileResponse();

            //Location loc = sp.idToLocation(id);
            if (detailPlace != null)
            {
                /*if (loc != null)
                {   
                        // Uncomment below to Test ImageGrab
                        
                }*/
                detailPlace.PublicCheckins++;
                TableOperation replaceOperation;
                replaceOperation = TableOperation.Replace(detailPlace);
                PlacesTable.Execute(replaceOperation);

                detailMobileResponse.Id = detailPlace.Id;
                detailMobileResponse.Lat = detailPlace.Lat;
                detailMobileResponse.Lng = detailPlace.Lng;
                detailMobileResponse.Name = detailPlace.Name;
                detailMobileResponse.Types = detailPlace.Types;
                detailMobileResponse.Address = detailPlace.Address;
                detailMobileResponse.PhoneNumber = detailPlace.PhoneNumber;
                detailMobileResponse.Rating = detailPlace.Rating;
                detailMobileResponse.OpeningHours = JsonConvert.DeserializeObject<List<string>>(detailPlace.OpeningHours);
                detailMobileResponse.Website = detailPlace.Website;
                detailMobileResponse.Reviews = JsonConvert.DeserializeObject<List<string>>(detailPlace.Reviews);
                detailMobileResponse.PublicCheckins = detailPlace.PublicCheckins;
                detailMobileResponse.FriendsCheckins = detailPlace.FriendsCheckins;
                detailMobileResponse.PhotoString = ImageDownload(detailPlace.PhotoBlobRef);
                detailMobileResponse.StreetViewString = ImageDownload(detailPlace.StreetViewBlobRef);

                azureDetailResult.detailPlaces.Add(detailMobileResponse);
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

        // get saved image in Azure blobStorage
        // if image not in blobStorage, grab from Google Streetview
        // then save image to blobStorage
        private string ImageGrab(Location loc)
        {
            string blob = loc.lat.ToString() + "," + loc.lng.ToString() + ".jpg"; 
            CloudBlockBlob blockBlob = streetViewBlobContainer.GetBlockBlobReference(blob);
            Byte[] barray;
            string base64String;

            var memoryStream = new MemoryStream();
            blockBlob.DownloadToStream(memoryStream);

            if (memoryStream.Length != 0)
            {
                base64String = Convert.ToBase64String(memoryStream.ToArray());
            }
            else
            {
                string url = "http://maps.googleapis.com/maps/api/streetview?size=300x100" +
                    "&location=" + loc.lat.ToString() + "," + loc.lng.ToString() +
                    "&sensor=false&key=" + "AIzaSyDN_W-Ya3sz38jT2zMRVQZLvbajQlyjbEA";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
                HttpResponseMessage httpResponse = client.GetAsync(url).Result;
                barray = httpResponse.Content.ReadAsByteArrayAsync().Result;
                base64String = Convert.ToBase64String(barray);

                memoryStream.Write(barray, 0, barray.Length);
                blockBlob.UploadFromStream(memoryStream);
            }
            return base64String;
        }

        // code to convert base64String of the image to the actual image on the phone side
        // has been sent to Mark
        private Image stringToImg(string base64String)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);

            // Convert byte[] to Image
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }

        public AzureSearchResult GetList(double lat, double lon, double radius, string type)
        {
            //AzureSearchResult azureSearchResult = sp.getPlaceInfo(lat, lon, radius, type);

            //if (azureSearchResult != null) return azureSearchResult;

            string url = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?location=" +
                lat.ToString() + "," + lon.ToString() + "&radius=" + radius.ToString() +
                "&types=" + type + "&sensor=false&key=" + GoogleAPIKey;

            //Default: Get response from Google Places Nearby Search 
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(url).Result; // Blocking call!

            AzureSearchResult azureSearchResult = new AzureSearchResult();

            if (response.IsSuccessStatusCode)
            {
                GoogleResponse gResponse = response.Content.ReadAsAsync<GoogleResponse>().Result;

                // Message to put into Azure More Info Queue, for AR Worker Role to pick up
                InfoQueueItem infoQueueItem = new InfoQueueItem();

                foreach (GoogleResult gResult in gResponse.results)
                {
                    //Compile results to return to Mobile Client
                    Place place = new Place(gResult.id, gResult.geometry.location, gResult.name);
                    azureSearchResult.places.Add(place);

                    //Compile into a message to put into Azure More Info Queue, for AR Worker Role to pick up
                    string gTypes = JsonConvert.SerializeObject(gResult.types);
                    GooglePlaceTable gPlace = new GooglePlaceTable(gResult.id, gResult.geometry.location.lat, gResult.geometry.location.lng,
                                                                    gResult.name, gResult.rating, gResult.reference, gTypes);

                    infoQueueItem.newGooglePlaces.Add(gPlace);
                    // var insertOperation = TableOperation.Insert(gPlace);
                    // PlacesTable.Execute(insertOperation);             
                }

                //Push message into Azure More Info Queue, for AR Worker Role to pick up
                string infoQueueMessageStr = JsonConvert.SerializeObject(infoQueueItem);
                var infoQueueMessage = new CloudQueueMessage(infoQueueMessageStr);
                moreInfoPlaceQueue.AddMessage(infoQueueMessage);

                // Save result in cache
                //sp.saveResult(azureSearchResult, type);
                return azureSearchResult;
            }
            else
            {
                Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Could not contact Google Places API"));
            }
            
        }
    }
}
