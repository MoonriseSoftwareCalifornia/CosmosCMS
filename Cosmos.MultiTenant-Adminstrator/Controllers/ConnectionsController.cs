using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cosmos.DynamicConfig;

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
            return View(await _context.Connections.ToListAsync());
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

            return View(connection);
        }

        // GET: Connections/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Connections/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DomainName,DbConn,DbName,StorageConn,Customer,WebsiteUrl")] Connection connection)
        {
            if (ModelState.IsValid)
            {
                connection.Id = Guid.NewGuid();
                _context.Add(connection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(connection);
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
            return View(connection);
        }

        // POST: Connections/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,DomainName,DbConn,DbName,StorageConn,Customer,WebsiteUrl")] Connection connection)
        {
            if (id != connection.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(connection);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConnectionExists(connection.Id))
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
            return View(connection);
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

            return View(connection);
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
