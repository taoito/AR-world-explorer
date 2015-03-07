#region License
/******************************************************************************
 * COPYRIGHT © MICROSOFT CORP. 
 * MICROSOFT LIMITED PERMISSIVE LICENSE (MS-LPL)
 * This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
 * 1. Definitions
 * The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here as under U.S. copyright law.
 * A “contribution” is the original software, or any additions or changes to the software.
 * A “contributor” is any person that distributes its contribution under this license.
 * “Licensed patents” are a contributor’s patent claims that read directly on its contribution.
 * 2. Grant of Rights
 * (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
 * (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
 * 3. Conditions and Limitations
 * (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or trademarks.
 * (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
 * (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
 * (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
 * (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
 * (F) Platform Limitation- The licenses granted in sections 2(A) & 2(B) extend only to the software or derivative works that you create that run on a Microsoft Windows operating system product.
 ******************************************************************************/
#endregion // License

using System.Windows;
using System.Windows.Input;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Point = System.Windows.Point;
using GART;
using GART.Controls;
using GART.Data;
using System.Device.Location;
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Microsoft.Devices.Sensors;
using System.Windows.Threading;
using System.Runtime.Serialization.Json;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using WorldExplorer.DataStructure;
using WorldExplorer.Utilities;
using System.Threading;
using Facebook;

namespace WorldExplorer
{
    public partial class MainPage : PhoneApplicationPage
    {

        #region Member Variables
        private Double radius;

        private bool local = true;
        private bool restaurant = false;
        private bool facebook = false;
        private bool loggedIn = false;
        private bool isLoggingout = false;
        private bool isTokenExpired = false;
        private ARLabel updatingLabel;
        private string gettingType;
        private GeoCoordinate locationSinceLastRequest;
        private GeoCoordinate currentLocation;
        private ObservableCollection<ARItem> ARSavedCollection = new ObservableCollection<ARItem>();
        private System.Object ARItemsLock = new System.Object();

        Compass compass;
        DispatcherTimer timer;
        GeoCoordinateWatcher watcher;

        double magneticHeading;
        double trueHeading;
        double headingAccuracy;
        Vector3 rawMagnetometerReading;
        bool isDataValid;

        bool calibrating = false;

        Accelerometer accelerometer;

        private const string AppId = "202303636571744"; // AppId for facebook App

        private const string ExtendedPermissions = "user_about_me,read_stream,user_events,friends_events";

        private readonly FacebookClient _fb = new FacebookClient();

        private string fbAccessToken;
        private string fbId;
        private String fbHostUrl = "https://www.facebook.com/home.php";

        private static ManualResetEvent expiredTokenMRE = new ManualResetEvent(false);

        #endregion // Member Variables

        public class ImageData
        {
            public BitmapImage ImagePath
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }

            public ImageData()
            {

            }

            public ImageData (BitmapImage ImagePath, string Name) 
            {
                this.ImagePath = ImagePath;
                this.Name = Name;
            }

        }

        #region Constructors
        public MainPage()
        {
            InitializeComponent();
            Application.Current.Host.Settings.EnableFrameRateCounter = false;
        }
        #endregion // Constructors

        #region Internal Methods


        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (compass != null && compass.IsDataValid)
            {
                // Stop data acquisition from the compass.
                compass.Stop();
                timer.Stop();

                accelerometer.Stop();
            }

            else
            {
                if (compass == null)
                {
                    // Instantiate the compass.
                    compass = new Compass();

                    // Specify the desired time between updates. The sensor accepts
                    // intervals in multiples of 20 ms.
                    compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);

                    // The sensor may not support the requested time between updates.
                    // The TimeBetweenUpdates property reflects the actual rate.



                    compass.CurrentValueChanged +=
                                 new EventHandler<SensorReadingEventArgs<CompassReading>>(compass_CurrentValueChanged);
                    compass.Calibrate +=
                        new EventHandler<CalibrationEventArgs>(compass_Calibrate);

                    accelerometer = new Accelerometer();
                    accelerometer.CurrentValueChanged +=
                        new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
                    accelerometer.Start();

                }

            }

            try
            {
                compass.Start();
                timer.Start();
            }
            catch (InvalidOperationException)
            {
            }
        }

        void compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            // Note that this event handler is called from a background thread
            // and therefore does not have access to the UI thread. To update 
            // the UI from this handler, use Dispatcher.BeginInvoke() as shown.
            // Dispatcher.BeginInvoke(() => { statusTextBlock.Text = "in CurrentValueChanged"; });


            isDataValid = compass.IsDataValid;

            trueHeading = e.SensorReading.TrueHeading;
            magneticHeading = e.SensorReading.MagneticHeading;
            headingAccuracy = Math.Abs(e.SensorReading.HeadingAccuracy);
            rawMagnetometerReading = e.SensorReading.MagnetometerReading;

        }

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            Vector3 v = e.SensorReading.Acceleration;

            bool isCompassUsingNegativeZAxis = false;

            if (Math.Abs(v.Z) < Math.Cos(Math.PI / 4) &&
                          (v.Y < Math.Sin(7 * Math.PI / 4)))
            {
                isCompassUsingNegativeZAxis = true;
            }
        }


        void timer_Tick(object sender, EventArgs e)
        {
            if (calibrating)
            {
                if (headingAccuracy <= 10)
                {
                    calibrationTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    calibrationTextBlock.Text = "Complete!";
                }
                else
                {
                    calibrationTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    calibrationTextBlock.Text = headingAccuracy.ToString("0.0");
                }
            }
        }

        void compass_Calibrate(object sender, CalibrationEventArgs e)
        {
            Dispatcher.BeginInvoke(() => { calibrationStackPanel.Visibility = Visibility.Visible; });
            calibrating = true;

            ApplicationBar.IsVisible = false;
            ARDisplay.Visibility = System.Windows.Visibility.Collapsed;

            NameBox.Visibility = System.Windows.Visibility.Collapsed;
            //AddressBox.Visibility = System.Windows.Visibility.Collapsed;
            ImageMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
            StreetViewMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
            attendantListBox.Visibility = System.Windows.Visibility.Collapsed;
            DescriptionBox.Visibility = System.Windows.Visibility.Collapsed;
            Scroller.Visibility = System.Windows.Visibility.Collapsed;
            BackMoreInfoButton.Visibility = System.Windows.Visibility.Collapsed;
            FacebookLoginPage.Visibility = System.Windows.Visibility.Collapsed;
            RadiusSlider.Visibility = System.Windows.Visibility.Collapsed;
            SliderValue.Visibility = System.Windows.Visibility.Collapsed;
            BackOptionsButton.Visibility = System.Windows.Visibility.Collapsed;
            SetRadiusText.Visibility = System.Windows.Visibility.Collapsed;
            Facebook.Visibility = System.Windows.Visibility.Collapsed;
            Border.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void calibrationButton_Click(object sender, RoutedEventArgs e)
        {
            calibrationStackPanel.Visibility = Visibility.Collapsed;
            calibrating = false;

            ApplicationBar.IsVisible = true;
            ARDisplay.Visibility = System.Windows.Visibility.Visible;

            NameBox.Visibility = System.Windows.Visibility.Collapsed;
            //AddressBox.Visibility = System.Windows.Visibility.Collapsed;
            ImageMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
            StreetViewMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
            attendantListBox.Visibility = System.Windows.Visibility.Collapsed;
            DescriptionBox.Visibility = System.Windows.Visibility.Collapsed;
            Scroller.Visibility = System.Windows.Visibility.Collapsed;
            BackMoreInfoButton.Visibility = System.Windows.Visibility.Collapsed;
            FacebookLoginPage.Visibility = System.Windows.Visibility.Collapsed;
            RadiusSlider.Visibility = System.Windows.Visibility.Collapsed;
            SliderValue.Visibility = System.Windows.Visibility.Collapsed;
            BackOptionsButton.Visibility = System.Windows.Visibility.Collapsed;
            SetRadiusText.Visibility = System.Windows.Visibility.Collapsed;
            Facebook.Visibility = System.Windows.Visibility.Collapsed;
            Border.Visibility = System.Windows.Visibility.Collapsed;

        }

        private void CompassCalibrate_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(() => { calibrationStackPanel.Visibility = Visibility.Visible; });
            calibrating = true;
        }


        private void ReadWebRequestCallback(IAsyncResult callbackResult)
        {

            try
            {
                //System.Diagnostics.Debug.WriteLine("Reading http response");

                HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.EndGetResponse(callbackResult);

                StreamReader httpwebStreamReader = new StreamReader(myResponse.GetResponseStream());

                string jsonResponse = httpwebStreamReader.ReadToEnd();

                //System.Diagnostics.Debug.WriteLine("Response from server: {0}", jsonResponse);

                MemoryStream memStream = new MemoryStream(Encoding.Unicode.GetBytes(jsonResponse));

                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BasicCloudResponse));

                BasicCloudResponse res = (BasicCloudResponse)ser.ReadObject(memStream);

                // There's some error that should be handled
                if (res.error != null)
                {
                    switch (res.error)
                    {
                        case "OAuthException":
                            // If user already logs in, just automatically log him in again
                            if (loggedIn)
                            {
                                isTokenExpired = true;
                                expiredTokenMRE.Reset();
                                Dispatcher.BeginInvoke(() => LoginFacebook());
                                while (isTokenExpired)
                                {
                                    expiredTokenMRE.WaitOne();
                                }

                                // When it is signaled, which means we have a fresh token, try to get fb events again
                                // TODO: check this when adding more functionalities
                                Dispatcher.BeginInvoke(() => AddFacebookLabels());
                                return;
                            }
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine("Error {0} was not handled", res.error);
                            return;
                    }
                    
                }
                //Dispatcher.BeginInvoke(() => AddLabel(ARDisplay.Location, ARDisplay.Location.Longitude.ToString()));

                GeoCoordinate tempGeo;

                List<Location> locationList = res.places;

                List<GeoCoordinate> geoList = new List<GeoCoordinate>();
                for (int i = 0; i < res.places.Count; i++)
                {
                    tempGeo = new GeoCoordinate(res.places[i].location.lat, res.places[i].location.lng);
                    geoList.Add(tempGeo);
                }

                Dispatcher.BeginInvoke(() => AddLabelFromList(geoList, locationList));

                myResponse.Close();
            }
            catch (WebException e)
            {
                System.Diagnostics.Debug.WriteLine("ReadWebRequestCallback WebException raised");
                System.Diagnostics.Debug.WriteLine("Message: {0}", e.Message);
                System.Diagnostics.Debug.WriteLine("Status: {0}", e.Status);
            }
        }

        private void ReadMoreDetailsRequestCallback(IAsyncResult callbackResult)
        {
            try
            {
                //System.Diagnostics.Debug.WriteLine("Reading http response");

                HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;
                HttpWebResponse myResponse = (HttpWebResponse)myRequest.EndGetResponse(callbackResult);

                StreamReader httpwebStreamReader = new StreamReader(myResponse.GetResponseStream());

                string jsonResponse = httpwebStreamReader.ReadToEnd();

                System.Diagnostics.Debug.WriteLine("Response from server: {0}", jsonResponse);

                MemoryStream memStream = new MemoryStream(Encoding.Unicode.GetBytes(jsonResponse));

                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(MoreDetailsResponse));

                MoreDetailsResponse res = (MoreDetailsResponse)ser.ReadObject(memStream);

                if (res.detailPlaces != null && res.detailPlaces.Count > 0)
                {
                    MoreDetailsMobileResponse location = res.detailPlaces[0];

                    Dispatcher.BeginInvoke(() => UpdateLocation(location));
                }
                else if (res.detailEvents != null && res.detailEvents.Count > 0)
                {
                    MoreDetailsMobileResponse location = res.detailEvents[0];

                    Dispatcher.BeginInvoke(() => UpdateLocation(location));
                }

                myResponse.Close();
            }
            catch (WebException e)
            {
                System.Diagnostics.Debug.WriteLine("ReadWebRequestCallback WebException raised");
                System.Diagnostics.Debug.WriteLine("Message: {0}", e.Message);
                System.Diagnostics.Debug.WriteLine("Status: {0}", e.Status);
            }
        }

        private void AddNearbyLabels(string type)
        {
            // Start with the current location
            GeoCoordinate current = ARDisplay.Location;
            locationSinceLastRequest = current;

            double rad;

            if (radius == 0)
            {
                // default value
                rad = 100.0;
            }
            else 
            {
                rad = radius;
            }

            string requestString = Communication.CreateLocationRequest(current.Latitude, current.Longitude, rad, type);

            System.Uri targetUri = new System.Uri(requestString);

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUri);
                System.Diagnostics.Debug.WriteLine("Going to make a request: {0}", requestString);
                request.BeginGetResponse(new AsyncCallback(ReadWebRequestCallback), request);
            }
            catch (WebException e)
            {
                System.Diagnostics.Debug.WriteLine("AddNearbyLabels WebException raised");
                System.Diagnostics.Debug.WriteLine("Message: {0}", e.Message);
                System.Diagnostics.Debug.WriteLine("Status: {0}", e.Status);
            }
        }

        private void AddFacebookLabels()
        {
            // Start with the current location
            GeoCoordinate current = ARDisplay.Location;
            locationSinceLastRequest = current;

            double rad;

            if (radius == 0)
            {
                // default value
                rad = 100.0;
            }
            else
            {
                rad = radius;
            }

            string requestString = Communication.CreateEventRequest(current.Latitude, current.Longitude, rad, fbId, fbAccessToken);

            System.Uri targetUri = new System.Uri(requestString);

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUri);
                System.Diagnostics.Debug.WriteLine("Going to make a request: {0}", requestString);
                request.BeginGetResponse(new AsyncCallback(ReadWebRequestCallback), request);
            }
            catch (WebException e)
            {
                System.Diagnostics.Debug.WriteLine("AddFacebookLabels WebException raised");
                System.Diagnostics.Debug.WriteLine("Message: {0}", e.Message);
                System.Diagnostics.Debug.WriteLine("Status: {0}", e.Status);
            }
        }

        private void GetMoreInformation(ARLabel label)
        {
            string requestString;

            if (label.PhoneType == "facebook")
            {
                if (fbId == null)
                {
                    MessageBox.Show("Please log into Facebook from the options menu.");
                    return;
                }
                else
                {
                    requestString = Communication.CreateMoreDetailsEventsRequest(label.Info.id, fbId);
                }
            }
            else if (label.PhoneType == "custom")
            {
                return;
            }
            else
            {
                requestString = Communication.CreateMoreDetailsRequest(label.Info.id);
            }

            System.Uri targetUri;

            if (requestString == null)
            {
                return;
            }
            else
            {
                updatingLabel = label;

                targetUri = new System.Uri(requestString);
            }

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUri);
                System.Diagnostics.Debug.WriteLine("Going to make a request: {0}", requestString);
                request.BeginGetResponse(new AsyncCallback(ReadMoreDetailsRequestCallback), request);
            }
            catch (WebException e)
            {
                System.Diagnostics.Debug.WriteLine("AddNearbyLabels WebException raised");
                System.Diagnostics.Debug.WriteLine("Message: {0}", e.Message);
                System.Diagnostics.Debug.WriteLine("Status: {0}", e.Status);
            }
        }

        private void AddLabelFromList(List<GeoCoordinate> geoList, List<Location> locationList)
        {
            //ARDisplay.ARItems.Clear();
            ObservableCollection<ARItem> items = new ObservableCollection<ARItem>();

            lock (ARItemsLock)
            {
                // Add new ARItems.
                for (int i = 0; i < geoList.Count; i++)
                {
                    ARLabel ml = new ARLabel()
                    {
                        Info = locationList[i],
                        GeoLocation = geoList[i],
                        Name = locationList[i].name,
                        DisplayName = CreateDisplayName(locationList[i].name),
                        PhoneType = gettingType
                    };

                    // If this is an inital facebook request, save the item to the saved list.
                    if (!facebook && ml.PhoneType.Equals("facebook"))
                    {
                        ARSavedCollection.Add(ml);
                    }
                    else
                    {
                        items.Add(ml);
                    }
                }

                // If items is empty, don't bother.
                if (items.Count != 0)
                {
                    // Get existing ARItems.
                    foreach (var arItem in ARDisplay.ARItems)
                    {
                        items.Add(arItem);
                    }

                    // Display on the ARDisplay.
                    ARDisplay.ARItems = items;
                }
            }

        }

        private void RemoveLabels(string type)
        {
            ObservableCollection<ARItem> labels = new ObservableCollection<ARItem>();
            lock (ARItemsLock)
            {
                foreach (ARLabel arLabel in ARDisplay.ARItems)
                {
                    if (!(arLabel.PhoneType.Equals(type)))
                    {
                        //System.Diagnostics.Debug.WriteLine(arLabel.PhoneType + " " + arLabel.Name + "\n");
                        labels.Add(arLabel);
                    }
                }

                ARDisplay.ARItems = labels;
            }
        }

        private void UpdateLocation(MoreDetailsMobileResponse location)
        {
            lock (ARItemsLock)
            {
                if (location != null)
                {
                    updatingLabel.MoreInfo = location;

                    NameBox.Text = updatingLabel.DisplayName;
                    //AddressBox.Text = updatingLabel.MoreInfo.Address;
                    DescriptionBox.Text = "Address: " + updatingLabel.MoreInfo.Address + "\n\n" + BuildMoreInfoDescriptionBox(updatingLabel);

                    if (!string.IsNullOrEmpty(updatingLabel.MoreInfo.PhotoString))
                    {
                        ImageMoreInfo.Source = StringToImg(updatingLabel.MoreInfo.PhotoString);
                        ImageMoreInfo.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        ImageMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    
                    if (!string.IsNullOrEmpty(updatingLabel.MoreInfo.StreetViewString))
                    {
                        StreetViewMoreInfo.Source = StringToImg(updatingLabel.MoreInfo.StreetViewString);
                        StreetViewMoreInfo.Visibility = System.Windows.Visibility.Visible;
                    }
                    
                    else 
                    {
                        StreetViewMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
                    }

                    if (updatingLabel.MoreInfo.AttendantList != null && updatingLabel.MoreInfo.AttendantList.Count > 0)
                    {
                        attendantListBox.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        attendantListBox.Visibility = System.Windows.Visibility.Collapsed;
                    }

                    ApplicationBar.IsVisible = false;
                    ARDisplay.Visibility = System.Windows.Visibility.Collapsed;

                    NameBox.Visibility = System.Windows.Visibility.Visible;
                    //AddressBox.Visibility = System.Windows.Visibility.Visible;
                    DescriptionBox.Visibility = System.Windows.Visibility.Visible;
                    Scroller.Visibility = System.Windows.Visibility.Visible;
                    
                    BackMoreInfoButton.Visibility = System.Windows.Visibility.Visible;

                    updatingLabel.PhoneType = updatingLabel.PhoneType;
                }
            }
        }

        private string BuildMoreInfoDescriptionBox(ARLabel label)
        {
            string descriptionString = "";

            if (!string.IsNullOrEmpty(label.MoreInfo.Description))
            {
                descriptionString += "Description: " + updatingLabel.MoreInfo.Description;
                descriptionString += "\n\n";
            }

            //if (label.MoreInfo.PublicCheckins > 0)
            //{

                //descriptionString += "People Attending: \n" + label.MoreInfo.PublicCheckins.ToString();
                //descriptionString += "\n\n";
            //}

            if (!string.IsNullOrEmpty(label.MoreInfo.HostName))
            {
                descriptionString += "Hosted by " + updatingLabel.MoreInfo.HostName;
                descriptionString += "\n\n";
            }

            if (!string.IsNullOrEmpty(label.MoreInfo.StartTime))
            {
                descriptionString += "Start: " + updatingLabel.MoreInfo.StartTime;
                descriptionString += "\n";
            }

            if (!string.IsNullOrEmpty(label.MoreInfo.EndTime))
            {
                descriptionString += "End: " + updatingLabel.MoreInfo.EndTime;
                descriptionString += "\n\n";
            }

            if (label.MoreInfo.Reviews != null && label.MoreInfo.Reviews.Count > 0)
            {
                string buildString = "";

                foreach (string s in updatingLabel.MoreInfo.Reviews)
                {
                    buildString += s + "\n\n";
                }

                descriptionString += "Reviews: \n" + buildString;
                descriptionString += "\n\n";
            }

            if (label.MoreInfo.Rating != 0)
            {
                descriptionString += "Rating: " + updatingLabel.MoreInfo.Rating.ToString() + "/5";
                descriptionString += "\n\n";
            }

            if (label.MoreInfo.OpeningHours != null && label.MoreInfo.OpeningHours.Count > 0)
            {
                string buildString = "";

                foreach (string s in updatingLabel.MoreInfo.OpeningHours)
                {
                    buildString += s + "\n";
                }

                descriptionString += "Hours: \n" + buildString;
                descriptionString += "\n\n";
            }

            if (!string.IsNullOrEmpty(label.MoreInfo.PhoneNumber))
            {
                descriptionString += "Phone Number: " + updatingLabel.MoreInfo.PhoneNumber;
                descriptionString += "\n\n";
            }

            if (!string.IsNullOrEmpty(label.MoreInfo.Website))
            {
                descriptionString += "Website: \n" + updatingLabel.MoreInfo.Website;
                descriptionString += "\n\n";
            }

            if (label.MoreInfo.AttendantList != null && label.MoreInfo.AttendantList.Count > 0)
            {
                string buildString = "";

                //foreach (string s in label.MoreInfo.AttendantList)
                //{
                //    buildString += s + "\n";
                //}

                descriptionString += "Friends Attending: \n" + buildString;
                descriptionString += "\n\n";

                List<ImageData> dataSource = new List<ImageData>();

                for (int i = 0; i < label.MoreInfo.AttendantList.Count; i++)
                {
                    ImageData newImg = new ImageData(StringToImg(label.MoreInfo.AttendantPicList[i]), label.MoreInfo.AttendantList[i]);
                    dataSource.Add(newImg);
                }
                attendantListBox.DataContext = dataSource;
            }
            else
            {
                descriptionString += "Public Checkins: " + updatingLabel.MoreInfo.PublicCheckins + "\n\n";
                descriptionString += "Friends Checkins: " + updatingLabel.MoreInfo.FriendsCheckins;
                descriptionString += "\n\n";
            }

            if (string.IsNullOrEmpty(descriptionString))
            {
                descriptionString += "No additional information to display.\n";
            }

            return descriptionString;
        }

        // code to convert base64String of the image to the actual image on the phone side
        private BitmapImage StringToImg(string base64String)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);

            // Convert byte[] to Image
            ms.Write(imageBytes, 0, imageBytes.Length);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(ms);
            return bitmapImage;
        }

        private string CreateDisplayName(string name)
        {
            if (name.Length > 35)
            {
                return name.Substring(0, 35) + "...";
            }
            else 
            {
                return name;
            }
        }

        /// <summary>
        /// Switches between rottaing the Heading Indicator or rotating the Map to the current heading.
        /// </summary>
        private void SwitchHeadingMode()
        {
            if (HeadingIndicator.RotationSource == RotationSource.AttitudeHeading)
            {
                HeadingIndicator.RotationSource = RotationSource.North;
                OverheadMap.RotationSource = RotationSource.AttitudeHeading;
            }
            else
            {
                OverheadMap.RotationSource = RotationSource.North;
                HeadingIndicator.RotationSource = RotationSource.AttitudeHeading;
            }
        }

        private Uri GetFacebookLoginUrl(string appId, string extendedPermissions)
        {
            var parameters = new Dictionary<string, object>();
            parameters["client_id"] = appId;
            parameters["redirect_uri"] = "https://www.facebook.com/connect/login_success.html";
            parameters["response_type"] = "token";
            parameters["display"] = "touch";

            // add the 'scope' only if we have extendedPermissions.
            if (!string.IsNullOrEmpty(extendedPermissions))
            {
                // A comma-delimited list of permissions
                parameters["scope"] = extendedPermissions;
            }

            return _fb.GetLoginUrl(parameters);
        }

        // When the user successfully log in, we go back to main page 
        // along with access token and user id
        private void LoginSucceded(string accessToken)
        {
            var fb = new FacebookClient(accessToken);

            MessageBox.Show("You have successfully logged into your facebook account");

            fb.GetCompleted += (o, e) =>
            {
                if (e.Error != null)
                {
                    Dispatcher.BeginInvoke(() => MessageBox.Show(e.Error.Message));
                    return;
                }

                var result = (IDictionary<string, object>)e.GetResultData();
                var id = (string)result["id"];

                //var url = string.Format("/MainPage.xaml?access_token={0}&id={1}", accessToken, id);

                //Dispatcher.BeginInvoke(() => NavigationService.Navigate(new Uri(url, UriKind.Relative)));

                fbAccessToken = accessToken;
                fbId = id;

                Dispatcher.BeginInvoke(() =>
                {
                    ApplicationBar.IsVisible = true;
                    ARDisplay.Visibility = System.Windows.Visibility.Visible;

                    FacebookLoginPage.Visibility = System.Windows.Visibility.Collapsed;
                    RadiusSlider.Visibility = System.Windows.Visibility.Collapsed;
                    SliderValue.Visibility = System.Windows.Visibility.Collapsed;
                    BackOptionsButton.Visibility = System.Windows.Visibility.Collapsed;
                    SetRadiusText.Visibility = System.Windows.Visibility.Collapsed;
                    Facebook.Visibility = System.Windows.Visibility.Collapsed;
                    Border.Visibility = System.Windows.Visibility.Collapsed;
                    Facebook.Content = "Logout of Facebook";
                    Facebook.Click -= LoginFacebookButton_Click;
                    Facebook.Click += LogoutFacebookButton_Click;

                    loggedIn = true;

                    // If we are re logging, signal the waiting thread
                    isTokenExpired = false;
                    expiredTokenMRE.Set();

                });

            };
            fb.GetAsync("me?fields=id");

            /*
             * Initial login request.
             */
            //AddFacebookLabels();
        }

        private void LogoutFacebook()
        {
            Uri logoutUrl = new Uri(String.
                Format("https://www.facebook.com/logout.php?next={0}&access_token={1}", fbHostUrl, this.fbAccessToken));
            
            System.Diagnostics.Debug.WriteLine("Logging out of fb account");
            this.isLoggingout = true;
            FacebookLoginPage.Navigate(logoutUrl);
            
            loggedIn = false;

        }

        private void LoginFacebook()
        {
            var loginUrl = GetFacebookLoginUrl(AppId, ExtendedPermissions);
            FacebookLoginPage.Navigate(loginUrl);
        }

        private void StartARRequests()
        {
            if (local)
            {
                gettingType = "local";
                AddNearbyLabels(null);
            }

            if (restaurant)
            {
                gettingType = "food";
                AddNearbyLabels("food");
            }

            if (facebook && loggedIn)
            {
                gettingType = "facebook";
                AddFacebookLabels();
            }
        }

        #endregion // Internal Methods

        
        #region Overrides / Event Handlers

        private void SampleMoreInfo_Click(object sender, RoutedEventArgs e)
        {
            Button moreInfoButton = (Button)sender;
            string title = moreInfoButton.CommandParameter.ToString();

            ARLabel displayItem = null;

            foreach (ARItem item in ARDisplay.ARItems)
            {
                ARLabel label = (ARLabel)item;

                if (label.PhoneType != "custom" && label.Info.name == title)
                {
                    displayItem = label;
                    break;
                }
            }

            if (displayItem.PhoneType != "custom")
            {
                if (displayItem.MoreInfo != null)
                {
                    ApplicationBar.IsVisible = false;
                    ARDisplay.Visibility = System.Windows.Visibility.Collapsed;

                    NameBox.Visibility = System.Windows.Visibility.Visible;
                    //AddressBox.Visibility = System.Windows.Visibility.Visible;
                    ImageMoreInfo.Visibility = System.Windows.Visibility.Visible;
                    StreetViewMoreInfo.Visibility = System.Windows.Visibility.Visible;
                    DescriptionBox.Visibility = System.Windows.Visibility.Visible;
                    Scroller.Visibility = System.Windows.Visibility.Visible;
                    BackMoreInfoButton.Visibility = System.Windows.Visibility.Visible;
                }
                else if (displayItem != null && displayItem.Info.id != null)
                {
                    updatingLabel = displayItem;
                    GetMoreInformation(displayItem);
                }
            }


        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Stop AR services
            //ARDisplay.StopServices();

            if (watcher != null)
            {
                watcher.Stop();
            }

            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Start AR services
            ARDisplay.StartServices();

            System.Diagnostics.Debug.WriteLine("Started service ");

            base.OnNavigatedTo(e);

            // Set up GeoCoordinate watcher
            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.MovementThreshold = 0;
                watcher.Start();
            }

        }

        private void OptionsButton_Click(object sender, System.EventArgs e)
        {
            ApplicationBar.IsVisible = false;
            ARDisplay.Visibility = System.Windows.Visibility.Collapsed;

            RadiusSlider.Visibility = System.Windows.Visibility.Visible;
            SliderValue.Visibility = System.Windows.Visibility.Visible;
            BackOptionsButton.Visibility = System.Windows.Visibility.Visible;
            SetRadiusText.Visibility = System.Windows.Visibility.Visible;
            Facebook.Visibility = System.Windows.Visibility.Visible;
            Border.Visibility = System.Windows.Visibility.Visible;

        }


        private void StartARButton_Click(object sender, EventArgs e)
        {
            // Turn these off so the user can see the tags initially.
            OverheadMap.Visibility = System.Windows.Visibility.Collapsed;
            HeadingIndicator.Visibility = System.Windows.Visibility.Collapsed;

            ARDisplay.ARItems.Clear();

            StartARRequests();
        }

        private void HeadingButton_Click(object sender, System.EventArgs e)
        {
            UIHelper.ToggleVisibility(HeadingIndicator);
        }

        private void MapButton_Click(object sender, System.EventArgs e)
        {
            UIHelper.ToggleVisibility(OverheadMap);
        }

        private void ShowLocal_Click(object sender, EventArgs e)
        {
            ApplicationBarMenuItem item = (ApplicationBarMenuItem)sender;

            if (local == true)
            {
                item.Text = "show local sites";
                RemoveLabels("local");
                local = false;
            }
            else 
            {
                item.Text = "hide local sites";

                // When local caching is implemented there will be no need to make a new request.
                gettingType = "local";
                AddNearbyLabels(null);

                local = true;
            }
        }

        private void ShowRestaurant_Click(object sender, EventArgs e)
        {
            ApplicationBarMenuItem item = (ApplicationBarMenuItem)sender;

            if (restaurant == true)
            {
                item.Text = "show restaurants";
                RemoveLabels("food");
                restaurant = false;
            }
            else
            {
                item.Text = "hide restaurants";

                // When local caching is implemented there will be no need to make a new request.
                gettingType = "food";
                AddNearbyLabels("food");

                restaurant = true;
            }
        }

        private void ShowEvents_Click(object sender, EventArgs e)
        {
            ApplicationBarMenuItem item = (ApplicationBarMenuItem)sender;

            if (facebook == true)
            {
                item.Text = "show social events";
                RemoveLabels("facebook");
                facebook = false;
            }
            else
            {
                if (!loggedIn)
                {
                    MessageBox.Show("Please log into Facebook in the options menu.");
                }
                else
                {
                    item.Text = "hide social events";

                    // When local caching is implemented there will be no need to make a new request.
                    gettingType = "facebook";
                    AddFacebookLabels();

                    facebook = true;
                }
            }
        }


        private void BackOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationBar.IsVisible = true;
            ARDisplay.Visibility = System.Windows.Visibility.Visible;

            RadiusSlider.Visibility = System.Windows.Visibility.Collapsed;
            SliderValue.Visibility = System.Windows.Visibility.Collapsed;
            BackOptionsButton.Visibility = System.Windows.Visibility.Collapsed;
            SetRadiusText.Visibility = System.Windows.Visibility.Collapsed;
            Facebook.Visibility = System.Windows.Visibility.Collapsed;
            Border.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoginFacebookButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationBar.IsVisible = false;
            ARDisplay.Visibility = System.Windows.Visibility.Collapsed;
            RadiusSlider.Visibility = System.Windows.Visibility.Collapsed;
            SliderValue.Visibility = System.Windows.Visibility.Collapsed;
            BackOptionsButton.Visibility = System.Windows.Visibility.Collapsed;
            SetRadiusText.Visibility = System.Windows.Visibility.Collapsed;
            Facebook.Visibility = System.Windows.Visibility.Collapsed;
            Border.Visibility = System.Windows.Visibility.Collapsed;

            FacebookLoginPage.Visibility = System.Windows.Visibility.Visible;

            LoginFacebook();
        }

        private void LogoutFacebookButton_Click(object sender, RoutedEventArgs e)
        {
            if (fbAccessToken == null)
            {
                System.Diagnostics.Debug.WriteLine("FB access token is null");
                MessageBox.Show("You haven't logged into your facebook account yet");
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("Log out access token:" + accessToken + "abc");

                ApplicationBar.IsVisible = false;
                ARDisplay.Visibility = System.Windows.Visibility.Collapsed;
                RadiusSlider.Visibility = System.Windows.Visibility.Collapsed;
                SliderValue.Visibility = System.Windows.Visibility.Collapsed;
                BackOptionsButton.Visibility = System.Windows.Visibility.Collapsed;
                SetRadiusText.Visibility = System.Windows.Visibility.Collapsed;
                Facebook.Visibility = System.Windows.Visibility.Collapsed;
                Border.Visibility = System.Windows.Visibility.Collapsed;

                FacebookLoginPage.Visibility = System.Windows.Visibility.Visible;

                LogoutFacebook();
            }
        }

        private void BackMoreInfoButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationBar.IsVisible = true;
            ARDisplay.Visibility = System.Windows.Visibility.Visible;

            NameBox.Visibility = System.Windows.Visibility.Collapsed;
            //AddressBox.Visibility = System.Windows.Visibility.Collapsed;
            ImageMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
            StreetViewMoreInfo.Visibility = System.Windows.Visibility.Collapsed;
            attendantListBox.Visibility = System.Windows.Visibility.Collapsed;
            DescriptionBox.Visibility = System.Windows.Visibility.Collapsed;
            Scroller.Visibility = System.Windows.Visibility.Collapsed;
            BackMoreInfoButton.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void FacebookLoginPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void FacebookLoginPage_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (isLoggingout)
            {
                fbAccessToken = null;
                fbId = null;

                ApplicationBar.IsVisible = true;
                ARDisplay.Visibility = System.Windows.Visibility.Visible;

                FacebookLoginPage.Visibility = System.Windows.Visibility.Collapsed;
                Facebook.Content = "Connect to Facebook";
                Facebook.Click -= LogoutFacebookButton_Click;
                Facebook.Click += LoginFacebookButton_Click;
                isLoggingout = false;
                return;
            }
            FacebookOAuthResult oauthResult;
            if (!_fb.TryParseOAuthCallbackUrl(e.Uri, out oauthResult))
            {
                return;
            }

            if (oauthResult.IsSuccess)
            {
                var accessToken = oauthResult.AccessToken;

                System.Diagnostics.Debug.WriteLine(accessToken);

                LoginSucceded(accessToken);
            }
            else
            {
                // user cancelled
                MessageBox.Show(oauthResult.ErrorDescription);
            }
        }

        private void CustomTag_Click(object sender, EventArgs e)
        {
            lock (ARItemsLock)
            {
                currentLocation = watcher.Position.Location;
                System.Diagnostics.Debug.WriteLine("custom tag location:" + currentLocation.ToString());

                if (currentLocation != null)
                {
                    ObservableCollection<ARItem> items = new ObservableCollection<ARItem>();
                    ARLabel ml = new ARLabel()
                    {
                        Info = null,
                        GeoLocation = currentLocation,
                        Name = "Custom Tag",
                        DisplayName = "Custom Tag",
                        PhoneType = "custom"
                    };

                    items.Add(ml);

                    // Get existing ARItems.
                    foreach (var arItem in ARDisplay.ARItems)
                    {
                        items.Add(arItem);
                    }

                    // Display on the ARDisplay.
                    ARDisplay.ARItems = items;
                }
            }
        }

        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            radius = e.NewValue;
        }

        private void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            System.Diagnostics.Debug.WriteLine(e.Position.Location.ToString());
            if (locationSinceLastRequest != null)
            {
                currentLocation = e.Position.Location;
                GeoCoordinate newCoord = e.Position.Location;

                double distance = locationSinceLastRequest.GetDistanceTo(newCoord);

                // If we are about to the end of the radius, make a new request.
                if (distance > (this.radius * 0.90))
                {
                    StartARRequests();
                }

            }

        }

        private void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {

        }

        #endregion // Overrides / Event Handlers



    }
}