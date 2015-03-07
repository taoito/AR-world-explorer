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

namespace WorldExplorer
{
    public partial class PlaceInformation : PhoneApplicationPage
    {

        private string title;

        public PlaceInformation()
        {
            InitializeComponent();

            this.Resources.Add("Title", title);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("placeName"))
            {
                title = NavigationContext.QueryString["placeName"];
            }

        }
    }
}