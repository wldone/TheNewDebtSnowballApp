using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DebtSnowballApp.Controllers
{
    public class QaDebtItemsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public QaDebtItemsController(ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Helper to ensure we always have a TempUserId in session
        private string? GetCurrentUserId()
        {
            return User?.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(User)
                : null;
        }
        private string GetOrCreateTempUserId()
        {
            var tempId = HttpContext.Session.GetString("TempUserId");
            if (string.IsNullOrEmpty(tempId))
            {
                tempId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("TempUserId", tempId);
            }
            return tempId;
        }
        private IQueryable<QaDebtItem> FilterForCurrentOwner(IQueryable<QaDebtItem> query)
        {
            var userId = GetCurrentUserId();

            if (userId != null)
            {
                return query.Where(d => d.UserId == userId);
            }

            var tempId = GetOrCreateTempUserId();
            return query.Where(d => d.TempUserId == tempId);
        }
        [AllowAnonymous]
        [HttpGet]
        public IActionResult QuickFromQa(string strategy = "Snowball", decimal extraPayment = 0)
        {
            // QaSnowball/Quick already knows how to use QA debts for logged-in
            // and demo data for anonymous. Just send them there.
            return RedirectToAction("Quick", "QaSnowball", new { strategy, extraPayment });
        }

        // FILL: copy the logged-in user's real debts into QA (optionally overwrite existing QA)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyFromMyDebts(bool overwrite = true)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Alert"] = "You must be logged in to copy your debts into QA.";
                return RedirectToAction(nameof(Index));
            }

            // load user's real debts
            var realDebts = await _db.DebtItems
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .ToListAsync();

            if (overwrite)
            {
                var existingQa = await _db.QaDebtItems.Where(q => q.UserId == userId).ToListAsync();
                if (existingQa.Count > 0)
                {
                    _db.QaDebtItems.RemoveRange(existingQa);
                }
            }

            // map DebtItem -> QaDebtItem
            var clones = realDebts.Select(d => new QaDebtItem
            {
                Name = d.Name,
                BegBalance = d.Balance,           // seed beginning with current
                Balance = d.Balance,
                InterestRate = d.InterestRate,
                MinimumPayment = d.MinimumPayment,
                UserId = userId
            }).ToList();

            if (clones.Count == 0)
            {
                TempData["Alert"] = "No debts found to copy.";
                return RedirectToAction(nameof(Index));
            }

            await _db.QaDebtItems.AddRangeAsync(clones);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Copied {clones.Count} debt(s) into QA.";
            return RedirectToAction(nameof(Index));
        }

        // Optional: let a user clear their QA set
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearMyQa()
        {
            var userId = _userManager.GetUserId(User);
            var mine = await _db.QaDebtItems.Where(q => q.UserId == userId).ToListAsync();
            if (mine.Count > 0)
            {
                _db.QaDebtItems.RemoveRange(mine);
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "QA debts cleared.";
            return RedirectToAction(nameof(Index));
        }
        // ========= INDEX (LIST) =========
        public async Task<IActionResult> Index(string sortOrder)
        {
            var userId = GetOrCreateTempUserId();

            ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "name";
            ViewData["BalanceSort"] = sortOrder == "balance" ? "balance_desc" : "balance";
            ViewData["RateSort"] = sortOrder == "rate" ? "rate_desc" : "rate";

            var qaDebts = FilterForCurrentOwner(_db.QaDebtItems.AsQueryable());

            qaDebts = sortOrder switch
            {
                "name" => qaDebts.OrderBy(d => d.Name),
                "name_desc" => qaDebts.OrderByDescending(d => d.Name),

                "balance" => qaDebts.OrderBy(d => d.Balance),
                "balance_desc" => qaDebts.OrderByDescending(d => d.Balance),

                "rate" => qaDebts.OrderBy(d => d.InterestRate),
                "rate_desc" => qaDebts.OrderByDescending(d => d.InterestRate),

                _ => qaDebts.OrderBy(d => d.Name)
            };

            return View(await qaDebts.ToListAsync());
        }

        // ========= DETAILS =========
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var qaDebts = FilterForCurrentOwner(_db.QaDebtItems.AsQueryable());
            var item = await qaDebts.FirstOrDefaultAsync(d => d.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        // ========= CREATE =========
        [HttpGet]
        public IActionResult Create()
        {
            return View(new QaDebtItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QaDebtItem qaDebtItem)
        {
            var userId = GetCurrentUserId();
            var tempId = userId == null ? GetOrCreateTempUserId() : null;

            if (!ModelState.IsValid)
            {
                return View(qaDebtItem);
            }

            if (userId != null)
            {
                qaDebtItem.UserId = userId;
                qaDebtItem.TempUserId = null;
            }
            else
            {
                qaDebtItem.UserId = null;
                qaDebtItem.TempUserId = tempId;
            }

            _db.QaDebtItems.Add(qaDebtItem);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ========= EDIT =========
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var qaDebts = FilterForCurrentOwner(_db.QaDebtItems.AsQueryable());
            var item = await qaDebts.FirstOrDefaultAsync(d => d.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QaDebtItem qaDebtItem)
        {
            if (id != qaDebtItem.Id)
                return BadRequest();

            var qaDebts = FilterForCurrentOwner(_db.QaDebtItems.AsQueryable());
            var existing = await qaDebts.FirstOrDefaultAsync(d => d.Id == id);

            if (existing == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                return View(qaDebtItem);
            }

            // Editable fields
            existing.Name = qaDebtItem.Name;
            existing.Balance = qaDebtItem.Balance;
            existing.InterestRate = qaDebtItem.InterestRate;
            existing.MinimumPayment = qaDebtItem.MinimumPayment;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ========= DELETE =========
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var qaDebts = FilterForCurrentOwner(_db.QaDebtItems.AsQueryable());
            var item = await qaDebts.FirstOrDefaultAsync(d => d.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var qaDebts = FilterForCurrentOwner(_db.QaDebtItems.AsQueryable());
            var item = await qaDebts.FirstOrDefaultAsync(d => d.Id == id);

            if (item != null)
            {
                _db.QaDebtItems.Remove(item);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ========= QUICK ANALYSIS =========
        [HttpGet]
        public async Task<IActionResult> QuickAnalysis()
        {
            // Get the current user's or temp session's debts
            var qaDebts = FilterForCurrentOwner(_db.QaDebtItems.AsQueryable());

            var items = await qaDebts
                .OrderBy(d => d.Balance)
                .ToListAsync();

            if (!items.Any())
            {
                TempData["Alert"] = "You need to add at least one debt before running Quick Analysis.";
                return RedirectToAction(nameof(Index));
            }

            // Here, you could calculate summary info — for now we just pass the list
            return View("QuickAnalysis", items);
        }

    }
}
