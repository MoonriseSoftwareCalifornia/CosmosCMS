using Cosmos.DynamicConfig;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.MultiTenant_Adminstrator.Models
{
    /// <summary>
    /// View model for the connection settings.
    /// </summary>
    public class ConnectionViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionViewModel"/> class.
        /// </summary>
        public ConnectionViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionViewModel"/> class with the specified connection.
        /// </summary>
        /// <param name="connection"></param>
        public ConnectionViewModel(Connection connection)
        {
            Id = connection.Id;
            DomainNames = connection.DomainNames == null ? string.Empty : string.Join(", ", connection.DomainNames);
            DbConn = connection.DbConn;
            DbName = connection.DbName;
            StorageConn = connection.StorageConn;
            Customer = connection.Customer;
            ResourceGroup = connection.ResourceGroup;
            PublisherMode = connection.PublisherMode;
            WebsiteUrl = connection.WebsiteUrl;
        }

        [Key]
        [Display(Name = "ID")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the editor domain name of the connection.
        /// </summary>
        [Display(Name = "Editor Domain Names")]
        public string DomainNames { get; set; } = null!;

        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Database Connection String")]
        public string DbConn { get; set; } = null!;

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Database Name")]
        public string DbName { get; set; } = "cosmoscms";

        /// <summary>
        /// Gets or sets the storage connection string.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Storage Connection String")]
        public string StorageConn { get; set; } = null!;

        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        [Display(Name = "Website Owner Name")]
        public string? Customer { get; set; } = null;

        /// <summary>
        /// Gets or sets the resrouce group where the customer's resources are kept.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Customer Resource Group")]
        public string? ResourceGroup { get; set; } = null;

        /// <summary>
        /// Gets or sets the publisher mode.
        /// </summary>
        [AllowedValues("Static", "Decoupled", "Headless", "Hybrid", "Static-dynamic", "")]
        public string PublisherMode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the website URL.
        /// </summary>
        [Url]
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Website URL")]
        public string WebsiteUrl { get; set; } = null!;

        /// <summary>
        /// Converts the view model to a <see cref="Connection"/> object.
        /// </summary>
        /// <returns></returns>
        internal Connection ToConnection()
        {
            return new Connection
            {
                Id = Id,
                DomainNames = DomainNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim()).ToArray(),
                DbConn = DbConn,
                DbName = DbName,
                StorageConn = StorageConn,
                Customer = Customer,
                ResourceGroup = ResourceGroup,
                PublisherMode = PublisherMode,
                WebsiteUrl = WebsiteUrl
            };
        }
    }
}
