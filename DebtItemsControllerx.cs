using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DebtSnowballApp.Data;
using DebtSnowballApp.Models;


namespace DebtSnowballApp
{
    public class DebtItemsControllerx : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebtItemsControllerx(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DebtItems
        public async Task<IActionResult> Index()
        {
            return View(await _context.DebtItems.ToListAsync());
        }

        // GET: DebtItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debtItem = await _context.DebtItems
                .FirstOrDefaultAsync(m => m.Id == id);
            if (debtItem == null)
            {
                return NotFound();
            }

            return View(debtItem);
        }

        // GET: DebtItems/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DebtItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Entity,UserId")] DebtItem debtItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(debtItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(debtItem);
        }

        // GET: DebtItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debtItem = await _context.DebtItems.FindAsync(id);
            if (debtItem == null)
            {
                return NotFound();
            }
            return View(debtItem);
        }

        // POST: DebtItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Entity,UserId")] DebtItem debtItem)
        {
            if (id != debtItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(debtItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DebtItemExists(debtItem.Id))
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
            return View(debtItem);
        }

        // GET: DebtItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debtItem = await _context.DebtItems
                .FirstOrDefaultAsync(m => m.Id == id);
            if (debtItem == null)
            {
                return NotFound();
            }

            return View(debtItem);
        }

        // POST: DebtItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var debtItem = await _context.DebtItems.FindAsync(id);
            if (debtItem != null)
            {
                _context.DebtItems.Remove(debtItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DebtItemExists(int id)
        {
            return _context.DebtItems.Any(e => e.Id == id);
        }
    }
}
