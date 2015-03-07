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
using GART.Data;
using WorldExplorer.DataStructure;

namespace WorldExplorer
{
    public class ARLabel : ARItem
    {
        private Location info;

        public Location Info
        {
            get
            {
                return info;
            }
            set
            {
                if (info != value)
                {
                    info = value;
                    NotifyPropertyChanged(() => Info);
                }
            }
        }

        private MoreDetailsMobileResponse moreInfo;

        public MoreDetailsMobileResponse MoreInfo
        {
            get
            {
                return moreInfo;
            }
            set
            {
                if (moreInfo != value)
                {
                    moreInfo = value;
                    NotifyPropertyChanged(() => MoreInfo);
                }
            }
        }

        private FacebookEventMobileResponse eventInfo;

        public FacebookEventMobileResponse EventInfo
        {
            get
            {
                return eventInfo;
            }
            set
            {
                if (eventInfo != value)
                {
                    eventInfo = value;
                    NotifyPropertyChanged(() => EventInfo);
                }
            }
        }

        private string name;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged(() => Name);
                }
            }
        }

        private string displayName;

        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                if (displayName != value)
                {
                    displayName = value;
                    NotifyPropertyChanged(() => DisplayName);

                    Content = value;
                }
            }
        }

        private string phoneType;

        public string PhoneType
        {
            get
            {
                return phoneType;
            }
            set
            {
                if (phoneType != value)
                {
                    phoneType = value;
                    NotifyPropertyChanged(() => PhoneType);
                }
            }
        }

    }
}
