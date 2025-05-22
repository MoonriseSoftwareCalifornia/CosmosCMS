
namespace Cosmos.Common.Data
{
    /// <summary>
    /// Enumeration representing the status of a database.
    /// </summary>
    public enum DbStatus
    {
        /// <summary>
        /// Database does not exist.
        /// </summary>
        DoesNotExist = 0,

        /// <summary>
        /// Database exists with one or more users.
        /// </summary>
        ExistsWithUsers = 1,

        /// <summary>
        /// Database exists with no users.
        /// </summary>
        ExistsWithNoUsers = 2,

        /// <summary>
        /// Database exists but is missing one or more containers.
        /// </summary>
        ExistsWithMissingContainers = 3,
    }
}
