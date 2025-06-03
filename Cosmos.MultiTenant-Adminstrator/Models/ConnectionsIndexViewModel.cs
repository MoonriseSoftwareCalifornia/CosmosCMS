using Cosmos.DynamicConfig;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.MultiTenant_Adminstrator.Models
{
    public class ConnectionsIndexViewModel : Connection
    {
        public ConnectionsIndexViewModel() { }

        public ConnectionsIndexViewModel(Connection connection)
        {
            this.Id = connection.Id;
            this.DomainNames = connection.DomainNames;
            this.StorageConn = connection.StorageConn;
            this.DbConn = connection.DbConn;
            this.DbName = connection.DbName;
            this.PublisherMode = connection.PublisherMode;
            this.Customer = connection.Customer;
            this.ResourceGroup = connection.ResourceGroup;
            this.WebsiteUrl = connection.WebsiteUrl;
            this.OwnerEmail = connection.OwnerEmail;
        }

        /// <summary>
        /// Indicates if the database connection is OK.
        /// </summary>
        [Display(Name = "Database")]
        public bool DatabaseStatus { get; set; } = false;

        /// <summary>
        /// Indicates if the storage connection is OK.
        /// </summary>
        [Display(Name = "Storage")]
        public bool StorageStatus { get; set; } = false;
    }
}
