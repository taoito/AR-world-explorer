using System.ComponentModel.DataAnnotations;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ARWebRole.Models
{
    public class FacebookCheckinTable : TableEntity
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

        public string LocationName { get; set; }

        public string FriendName { get; set; }

        public string CheckinTime { get; set; }

        public string Message { get; set; }

    }
}