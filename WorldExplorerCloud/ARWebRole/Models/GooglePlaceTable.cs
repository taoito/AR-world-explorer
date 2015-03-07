using System.ComponentModel.DataAnnotations;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ARWebRole.Models
{
    public class GooglePlaceTable : TableEntity
    {
        [Required]
        public string Id
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }
        [Required]
        public double Lat { get; set; }
        [Required]
        public double Lng { get; set; }
        [Required]
        public string Name { get; set; }
        public string Reference { get; set; }
        public string Types { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public double Rating { get; set; }
        public string OpeningHours { get; set; }
        public string Website { get; set; }
        public string Reviews { get; set; }
        public string Events { get; set; }
        public int PublicCheckins { get; set; }
        public string FriendsCheckins { get; set; }
        public string PhotoBlobRef { get; set; }
        public string StreetViewBlobRef { get; set; }

        public GooglePlaceTable()
        {
            this.PartitionKey = "googleplace";
        }

        public GooglePlaceTable(string Id, double Lat, double Lng, string Name, double Rating, string Reference, string Types) 
                                //string Type, string Address, string PhoneNumber, string Rating,
                                //string OpeningHours, string Website, string Reviews, string Events)
        {
            this.PartitionKey = "googleplace";
            this.Id = Id;
            this.Lat = Lat;
            this.Lng = Lng;
            this.Name = Name;
            this.Rating = Rating;
            this.Reference = Reference;
            this.Types = Types;
        }
    }
}