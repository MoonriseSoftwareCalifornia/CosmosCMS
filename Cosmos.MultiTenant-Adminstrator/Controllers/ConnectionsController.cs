using Cosmos.DynamicConfig;
using Cosmos.MultiTenant_Adminstrator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cosmos.MultiTenant_Adminstrator.Controllers
{
    public class ConnectionsController : Controller
    {
        private readonly DynamicConfigDbContext _context;
                public ConnectionsController(DynamicConfigDbContext context)
        {
            _context = context;
        }

        // GET: Connections
        public async Task<IActionResult> Index()
        {
            await _context.Database.EnsureCreatedAsync();

            try
            {
                var connections = await _context.Connections.ToListAsync();
                if (connections == null || !connections.Any())
                {
                    return RedirectToAction("Create");
                }

                return View(await _context.Connections.ToListAsync());
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                ModelState.AddModelError(string.Empty, "An error occurred while retrieving connections: " + ex.Message);
            }

            return View(new List<Connection>());
        }

        // GET: Connections/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var connection = await _context.Connections
                .FirstOrDefaultAsync(m => m.Id == id);
            if (connection == null)
            {
                return NotFound();
            }

            return View(new ConnectionViewModel(connection));
        }

        // GET: Connections/Create
        public IActionResult Create()
        {
            return View(new ConnectionViewModel
            {
                Id = Guid.NewGuid(),
                DomainNames = string.Empty,
                DbConn = string.Empty,
                DbName = "cosmoscms",
                StorageConn = string.Empty,
                Customer = string.Empty,
                WebsiteUrl = string.Empty
            });
        }

        // POST: Connections/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DomainNames,DbConn,DbName,StorageConn,Customer,WebsiteUrl,ResourceGroup,PublisherMode")] ConnectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Id = Guid.NewGuid();
                _context.Add(model.ToConnection());
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Connections/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var connection = await _context.Connections.FindAsync(id);
            if (connection == null)
            {
                return NotFound();
            }
            return View(new ConnectionViewModel(connection));
        }

        // POST: Connections/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,DomainNames,DbConn,DbName,StorageConn,Customer,WebsiteUrl,ResourceGroup,PublisherMode")] ConnectionViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model.ToConnection());
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConnectionExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Connections/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var connection = await _context.Connections
                .FirstOrDefaultAsync(m => m.Id == id);
            if (connection == null)
            {
                return NotFound();
            }

            return View(new ConnectionViewModel(connection));
        }

        // POST: Connections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var connection = await _context.Connections.FindAsync(id);
            if (connection != null)
            {
                _context.Connections.Remove(connection);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ConnectionExists(Guid id)
        {
            return _context.Connections.Any(e => e.Id == id);
        }
    }
}
