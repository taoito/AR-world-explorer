using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using ARWebRole.Models;
using System.Diagnostics;

namespace ARWebRole.Controllers
{
    public class HomeController : Controller
    {
        private CloudTable POITable;

        public HomeController()
        {
            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();
            POITable = tableClient.GetTableReference("PlaceOfInterest");
        }

        //Retrieve a Google Place entry from the Azure Table
        private GooglePlaceTable FindPlace(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<GooglePlaceTable>(partitionKey, rowKey);
            var retrievedResult = POITable.Execute(retrieveOperation);
            var gPlace = retrievedResult.Result as GooglePlaceTable;
            return gPlace;
        }

        //Retrieve a Facebook Event entry from the Azure Table
        private FacebookEventTable FindEvent(string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<FacebookEventTable>(partitionKey, rowKey);
            var retrievedResult = POITable.Execute(retrieveOperation);
            var fbEvent = retrievedResult.Result as FacebookEventTable;
            return fbEvent;
        }

        public ActionResult Index()
        {
            //ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            TableRequestOptions reqOptions = new TableRequestOptions()
            {
                MaximumExecutionTime = TimeSpan.FromSeconds(1.5),
                RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3)
            };
            List<GooglePlaceTable> placeLists;

            try
            {
                var query = new TableQuery<GooglePlaceTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "googleplace"));
                placeLists = POITable.ExecuteQuery(query, reqOptions).ToList();
            }
            catch (StorageException se)
            {
                ViewBag.errorMessage = "Timeout error, try again. ";
                Trace.TraceError(se.Message);
                return View("Error");
            }

            return View(placeLists);
            //return View();

        }


        public ActionResult Delete(string partitionKey, string rowKey)
        {
            POITable.Execute(TableOperation.Delete(new TableEntity(partitionKey, rowKey) { ETag = "*" }));
            return RedirectToAction("Index");
        }

        public ActionResult MoreDetails()
        {
            TableRequestOptions reqOptions = new TableRequestOptions()
            {
                MaximumExecutionTime = TimeSpan.FromSeconds(1.5),
                RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3)
            };
            List<GooglePlaceTable> placeLists;

            try
            {
                var query = new TableQuery<GooglePlaceTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "googleplace"));
                placeLists = POITable.ExecuteQuery(query, reqOptions).ToList();
            }
            catch (StorageException se)
            {
                ViewBag.errorMessage = "Timeout error, try again. ";
                Trace.TraceError(se.Message);
                return View("Error");
            }
            return View("About",placeLists);
        }

        public ActionResult BasicInfo()
        {
            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            //ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            //ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
