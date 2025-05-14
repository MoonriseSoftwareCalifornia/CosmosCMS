namespace Cosmos.MultiTenant_Adminstrator.Controllers
{
    /// <summary>
    /// A utility class for formatting numbers, especially for displaying byte sizes in a human-readable format.
    /// </summary>
    public class NumberFormatter
    {
        public static string FormatBytes(double? bytes, bool useBinary = false)
        {
            if (!bytes.HasValue)
            {
                return string.Empty;
            }

            string[] suffixes = useBinary
                ? new[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB" }
                : new[] { "B", "KB", "MB", "GB", "TB", "PB" };

            int unit = useBinary ? 1024 : 1000;
            double size = bytes.Value;
            int i = 0;

            while (size >= unit && i < suffixes.Length - 1)
            {
                size /= unit;
                i++;
            }

            return $"{size:0.##} {suffixes[i]}";
        }

    }
}


