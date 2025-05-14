using System.ComponentModel.DataAnnotations;

namespace Cosmos.MultiTenant_Adminstrator.Models
{
    /// <summary>
    /// ViewModel for displaying metrics in the index view.
    /// </summary>
    public class MetricsIndexViewModel
    {
        private double OneMillion = 1000000;
        private double BytesPerGb = 1024 * 1024 * 1024;

        private double BlobStoragePerGB = 0.08;
        // private double BlobStorageTransactionPer10k = 0.055;

        private double FrontDoorEgressRatePerGB = 0.08;
        private double FrontDoorIngressRatePerGB = 0.02;

        private double DatabaseRuRatePer1million = 0.28;
        private double DatabaseStorageRatePerGB = 0.25;

        [Display(Name = "Date")]
        public DateTimeOffset TimeStamp { get; set; }

        [Display(Name = "URL")]
        public string WebsiteUrl { get; set; } = string.Empty;

        [Display(Name = "Customer")]
        public string Customer { get; set; } = string.Empty;

        [Display(Name = "DB RU Usage")]
        public double? SumDatabaseRuUsage { get; set; }

        [Display(Name = "DB RU Cost")]
        public double? DatabaseRuCost
        {
            get
            {
                if (SumDatabaseRuUsage.HasValue)
                {
                    return CalculateCostPerMillion(SumDatabaseRuUsage.Value, DatabaseRuRatePer1million);
                }
                return null;
            }
        }

        [Display(Name = "DB Data Usage")]
        public double? MaxDatabaseDataUsageBytes { get; set; }

        [Display(Name = "DB Storage Cost")]
        public double? DatabaseStorageCost { 
            get 
            {
                if (MaxDatabaseDataUsageBytes.HasValue)
                {
                    return CalculateCostPerBytes(MaxDatabaseDataUsageBytes.Value, DatabaseStorageRatePerGB);
                }
                return null;
            }
        }

        [Display(Name = "FD Response")]
        public long? SumFrontDoorResponseBytes { get; set; }

        [Display(Name = "FD Egress Cost")]
        public double? FrontDoorEgressCost
        { 
            get 
            {
                if (SumFrontDoorResponseBytes.HasValue)
                {
                    return CalculateCostPerBytes(SumFrontDoorResponseBytes.Value, FrontDoorEgressRatePerGB);
                }
                return null;
            }
        }

        [Display(Name = "FD Request")]
        public long? SumFrontDoorRequestBytes { get; set; }

        [Display(Name = "FD Ingress Cost")]
        public double? FrontDoorIngressCost
        {
            get
            {
                if (SumFrontDoorRequestBytes.HasValue)
                {
                    return CalculateCostPerBytes(SumFrontDoorRequestBytes.Value, FrontDoorIngressRatePerGB);
                }
                return null;
            }
        }

        [Display(Name = "Blob Storage Usage")]
        public double? MaxBlobStorageBytes { get; set; }

        [Display(Name = "Blob Storage Cost")]
        public double? BlobStorageCost
        {
            get
            {
                if (MaxBlobStorageBytes.HasValue)
                {
                    return CalculateCostPerBytes(MaxBlobStorageBytes.Value, BlobStoragePerGB);
                }
                return null;
            }
        }

        private double CalculateCostPerBytes(double bytes, double ratePerGB)
        {
            if (bytes <= 0)
            {
                return 0;
            }

            double gigabytes = bytes / BytesPerGb;
            return gigabytes * ratePerGB;
        }

        private double CalculateCostPerMillion(double units, double rate)
        {
            if (units <= 0)
            {
                return 0;
            }
            double millions = units / OneMillion;
            return millions * rate;
        }
    }
}