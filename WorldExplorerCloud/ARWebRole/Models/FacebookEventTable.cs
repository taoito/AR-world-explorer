using System.ComponentModel.DataAnnotations;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ARWebRole.Models
{
    public class FacebookEventTable : TableEntity
    {
        [Required]
        public string UserId
        {
            get
            {
                return this.PartitionKey;
            }
            set
            {
                this.PartitionKey = value;
            }
        }

        [Required]
        public string Id
        {
            get
            {
                return this.RowKey;
            }
            set
            {
                this.RowKey = value;
            }
        }

        [Required]
        public double Lat { get; set; }
        [Required]
        public double Lng { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string HostName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Address { get; set; }
        public string AttendantList { get; set; }
        public string AttendantPicList { get; set; }
        public string PhotoBlobRef { get; set; }
        public string StreetViewBlobRef { get; set; }
        public string AccessToken { get; set; }

        public FacebookEventTable() { }

        public FacebookEventTable(string UserId, string Id, double Lat, double Lng, string Name, string Description, string HostName, 
                                string Address, string StartTime, string EndTime, string Pic, string AccessToken) 
        {
            this.UserId = UserId;
            this.Id = Id;
            this.Lat = Lat;
            this.Lng = Lng;
            this.Name = Name;
            this.Description = Description;
            this.HostName = HostName;
            this.Address = Address;
            this.StartTime = StartTime;
            this.EndTime = EndTime;
            this.PhotoBlobRef = Pic;
            this.AccessToken = AccessToken;
        }
    }
}