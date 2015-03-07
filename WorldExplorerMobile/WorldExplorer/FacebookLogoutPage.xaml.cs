using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Facebook;

namespace WorldExplorer
{
    public partial class FacebookLogoutPage : PhoneApplicationPage
    {
        private readonly FacebookClient _fb = new FacebookClient();

        private string _accessToken;

        public FacebookLogoutPage()
        {
            InitializeComponent();
        }

        private void logoutFacebook()
        {
            string nextPage = "https://www.facebook.com/";
            System.Diagnostics.Debug.WriteLine("logging out fb account");
            webBrowser1.Navigate(new Uri(String.Format("https://www.facebook.com/logout.php?next={0}&access_token={1}", 
                nextPage, _accessToken)));
        }

        private void webBrowser1_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void webBrowser1_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //MessageBox.Show("You have successfully logged out your facebook account");
            System.Diagnostics.Debug.WriteLine("Logged out, going back to main page");
            var url = string.Format("/MainPage.xaml");
            NavigationService.Navigate(new Uri(url, UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("User is logging out");

            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("access_token"))
            {
                _accessToken = NavigationContext.QueryString["access_token"];
                if (_accessToken.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("access token is null");
                    MessageBox.Show("You haven't logged in your facebook account yet");
                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Log out access token:" + _accessToken + "abc");
                    logoutFacebook();
                }
            }
            else
            {
                MessageBox.Show("Access Token for facebook account is missing");
                var url = string.Format("/MainPage.xaml");
                NavigationService.Navigate(new Uri(url, UriKind.Relative));
            }
        }
    }
}