using DebtSnowballApp.Models;
using DebtSnowballApp.Areas.Admin.ViewModels;
using DebtSnowballApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtSnowballApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PartnersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PartnersController(ApplicationDbContext db) => _db = db;

        // GET: /Admin/Partners
        public async Task<IActionResult> Index(string? q)
        {
            var partners = _db.Partners.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                partners = partners.Where(p =>
                    p.Name.Contains(term) ||
                    (p.Code != null && p.Code.Contains(term)));
            }

            var list = await partners
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            ViewData["Toolbar"] = new AdminListToolbarModel
            {
                Title = "Manage Partners",
                CreateText = "Create Partner",
                CreateUrl = Url.Action("Create", "Partners", new { area = "Admin" })!,
                SearchPlaceholder = "Search partners…",
                QueryParamName = "q",
                CurrentQuery = q ?? ""
            };

            return View(list);
        }

        // GET: /Admin/Partners/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Partner { Active = true, SortOrder = 0 });
        }

        // POST: /Admin/Partners/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Code,SupportEmail,SupportPhone,Website,SortOrder,Active")] Partner model)
        {
            if (!ModelState.IsValid) return View(model);

            model.CreatedAt = DateTime.UtcNow;
            _db.Partners.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Partners/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var partner = await _db.Partners.FindAsync(id);
            if (partner == null) return NotFound();
            return View(partner);
        }

        // POST: /Admin/Partners/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,SupportEmail,SupportPhone,Website,SortOrder,Active,RowVersion")] Partner model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var dbEntity = await _db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (dbEntity == null) return NotFound();

            model.CreatedAt = dbEntity.CreatedAt;          // preserve original
            model.UpdatedAt = DateTime.UtcNow;

            _db.Entry(model).Property(p => p.RowVersion).OriginalValue = model.RowVersion!;
            _db.Update(model);

            try
            {
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Another user modified this record. Please reload and try again.");
                return View(model);
            }
        }

        // GET: /Admin/Partners/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var partner = await _db.Partners.FindAsync(id);
            if (partner == null) return NotFound();
            return View(partner);
        }

        // POST: /Admin/Partners/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var partner = await _db.Partners.FindAsync(id);
            if (partner != null)
            {
                _db.Partners.Remove(partner);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
