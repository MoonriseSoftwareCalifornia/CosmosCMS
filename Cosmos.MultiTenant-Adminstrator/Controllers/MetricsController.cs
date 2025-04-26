using Microsoft.AspNetCore.Mvc;

namespace Cosmos.MultiTenant_Adminstrator.Controllers
{
    public class MetricsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
