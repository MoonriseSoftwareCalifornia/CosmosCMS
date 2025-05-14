using Cosmos.DynamicConfig;
using Cosmos.MultiTenant_Adminstrator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cosmos.MultiTenant_Adminstrator.Controllers
{
    public class MetricsController : Controller
    {

        private readonly DynamicConfigDbContext _context;
        public MetricsController(DynamicConfigDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var connections = await _context.Connections.ToListAsync();
            if (connections == null || !connections.Any())
            {
                return RedirectToAction("Create", "Connections");
            }
            var monthStart = new DateTime(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, 1, 0, 0, 0);
            var metrics = await _context.Metrics.Where(w => w.TimeStamp >= monthStart).ToListAsync();

            var model = (from m in metrics
                         join c in connections
                         on m.ConnectionId equals c.Id
                         group m by new { m.TimeStamp.Date, c.WebsiteUrl, c.Customer } into g
                         select new MetricsIndexViewModel()
                         {
                             TimeStamp = g.Key.Date,
                             WebsiteUrl = g.Key.WebsiteUrl,
                             Customer = g.Key.Customer,
                             SumDatabaseRuUsage = g.Sum(x => x.DatabaseRuUsage),
                             MaxDatabaseDataUsageBytes = g.Max(x => x.DatabaseDataUsageBytes),
                             SumFrontDoorResponseBytes = g.Sum(x => x.FrontDoorResponseBytes),
                             SumFrontDoorRequestBytes = g.Sum(x => x.FrontDoorRequestBytes),
                             MaxBlobStorageBytes = g.Max(x => x.BlobStorageBytes),
                         });

            return View(model);
        }
    }
}
