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
    public partial class FacebookLoginPage : PhoneApplicationPage
    {
        private const string AppId = "202303636571744"; // AppId for facebook App

        private const string ExtendedPermissions = "user_about_me,read_stream,user_events,friends_events";

        private readonly FacebookClient _fb = new FacebookClient();

        public FacebookLoginPage()
        {
            InitializeComponent();
        }

        private void webBrowser1_Loaded(object sender, RoutedEventArgs e)
        {
            var loginUrl = GetFacebookLoginUrl(AppId, ExtendedPermissions);
            webBrowser1.Navigate(loginUrl);
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

        private void webBrowser1_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
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

        // When the user successfully log in, we go back to main page 
        // along with access token and user id
        private void LoginSucceded(string accessToken)
        {
            var fb = new FacebookClient(accessToken);

            MessageBox.Show("You have successfully logged in your facebook account");

            fb.GetCompleted += (o, e) =>
            {
                if (e.Error != null)
                {
                    Dispatcher.BeginInvoke(() => MessageBox.Show(e.Error.Message));
                    return;
                }

                var result = (IDictionary<string, object>)e.GetResultData();
                var id = (string)result["id"];

                var url = string.Format("/MainPage.xaml?access_token={0}&id={1}", accessToken, id);

                Dispatcher.BeginInvoke(() => NavigationService.Navigate(new Uri(url, UriKind.Relative)));
            };
            fb.GetAsync("me?fields=id");
        }
    }
}