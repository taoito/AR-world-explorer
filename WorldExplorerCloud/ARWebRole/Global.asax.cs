using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ARWebRole.Models;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

namespace ARWebRole
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static Stockpile sp;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterAuth();

            //Removes the XML Formatter from ASP.Net Web API. Makes JSON the default.
            GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Clear();

            // Verify that all of the tables, queues, and blob containers used in this application
            // exist, and create any that don't already exist.
            CreateTablesQueuesBlobContainers();

            // hashtable wherein results of GetList requests are stored
            sp = new Stockpile(); 
        }
        private static void CreateTablesQueuesBlobContainers()
        {
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            // If this is running in a Windows Azure Web Site (not a Cloud Service) use the Web.config file:
            //    var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var PlacesTable = tableClient.GetTableReference("PlacesTable");
            PlacesTable.CreateIfNotExists();
            var EventsTable = tableClient.GetTableReference("EventsTable");
            EventsTable.CreateIfNotExists();
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("photoblobcontainer");
            blobContainer.CreateIfNotExists();
            var blobContainer2 = blobClient.GetContainerReference("streetviewblobcontainer");
            blobContainer2.CreateIfNotExists();
            var queueClient = storageAccount.CreateCloudQueueClient();
            var infoPlaceQueue = queueClient.GetQueueReference("moreinfoplacequeue");
            infoPlaceQueue.CreateIfNotExists();
            var infoEventQueue = queueClient.GetQueueReference("moreinfoeventqueue");
            infoEventQueue.CreateIfNotExists();
        }
    }
}