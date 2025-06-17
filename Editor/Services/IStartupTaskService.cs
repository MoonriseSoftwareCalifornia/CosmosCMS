using System.Threading.Tasks;

namespace Cosmos.Editor.Services
{
    /// <summary>
    /// Service interface for running startup tasks asynchronously.
    /// </summary>
    public interface IStartupTaskService
    {
        Task RunAsync();
    }
}
