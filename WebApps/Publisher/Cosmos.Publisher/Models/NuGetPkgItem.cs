namespace Cosmos.Cms.Publisher.Models
{
    /// <summary>
    /// NuGet package item meta data
    /// </summary>
    public class NuGetPkgItem
    {
        /// <summary>
        /// Package ID
        /// </summary>
        public string? Id { get; set; }
        /// <summary>
        /// Version
        /// </summary>
        public string? Version { get; set; }
        /// <summary>
        /// Authors
        /// </summary>
        public string? Authors { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Download count
        /// </summary>
        public long? DownloadCount { get; set; }
        /// <summary>
        /// Project URL
        /// </summary>
        public string? ProjectUrl { get; set; }
        /// <summary>
        /// Package owners
        /// </summary>
        public string? Owners { get; set; }
    }
}
