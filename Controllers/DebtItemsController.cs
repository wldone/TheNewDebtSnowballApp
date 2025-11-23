using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DebtSnowballApp.Data;
using Microsoft.AspNetCore.Identity;
using DebtSnowballApp.Models;
using DebtSnowballApp.ViewModels;


namespace DebtSnowballApp.Controllers
{
    public class DebtItemsController : Controller
    {

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DebtItemsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }

        // GET: DebtItems
        public async Task<IActionResult> Index(string sortOrder)
        {
            var userId = _userManager.GetUserId(User);

            // keep track of current sort for the view
            ViewData["CurrentSort"] = sortOrder;

            ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "name";
            ViewData["TypeSort"] = sortOrder == "type" ? "type_desc" : "type";
            ViewData["BalanceSort"] = sortOrder == "balance" ? "balance_desc" : "balance";
            ViewData["RateSort"] = sortOrder == "rate" ? "rate_desc" : "rate";

            IQueryable<DebtItem> debts = _db.DebtItems
                .Where(d => d.UserId == userId)
                .Include(d => d.DebtType) // navigation prop — OK
                .Include(d => d.User);    // navigation prop — OK

            debts = sortOrder switch
            {
                "name" => debts.OrderBy(d => d.Name),
                "name_desc" => debts.OrderByDescending(d => d.Name),

                "type" => debts.OrderBy(d => d.DebtType.Description),
                "type_desc" => debts.OrderByDescending(d => d.DebtType.Description),

                "balance" => debts.OrderBy(d => d.Balance),
                "balance_desc" => debts.OrderByDescending(d => d.Balance),

                "rate" => debts.OrderBy(d => d.InterestRate),
                "rate_desc" => debts.OrderByDescending(d => d.InterestRate),

                // default: sort by Name ascending
                _ => debts.OrderBy(d => d.Name),
            };

            return View(await debts.ToListAsync());
        }

        // GET: DebtItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debtItem = await _db.DebtItems
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
            var viewModel = new DebtItemViewModel
            {
                DebtTypeList = _db.DebtTypes
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Description
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // POST: DebtItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DebtItemViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.PartnerId == null)
            {
                ModelState.AddModelError("", "User or Partner not available.");

                // Rehydrate dropdown so the view doesn't break
                vm.DebtTypeList = await _db.DebtTypes
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Description
                    }).ToListAsync();

                return View(vm);
            }

            var debtItem = vm.DebtItem;
            debtItem.UserId = user.Id;
            debtItem.BegBalance = debtItem.Balance;
            debtItem.CreationDate = DateTime.UtcNow;
            debtItem.LastUpdate = DateTime.UtcNow;

            ModelState.Remove("DebtItem.UserId");
            // temp until full create debt form
            //ModelState.Remove("DebtItem.DueDate");
            //ModelState.Remove("DebtItem.SendDate");

            if (!TryValidateModel(debtItem, prefix: "DebtItem"))
            {
                // repop dropdowns etc.
                vm.DebtTypeList = await _db.DebtTypes
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Description })
                    .ToListAsync();

                return View(vm);
            }
            _db.Add(debtItem);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        // GET: DebtItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var debtItem = await _db.DebtItems.FindAsync(id);
            if (debtItem == null)
                return NotFound();

            var viewModel = new DebtItemViewModel
            {
                DebtItem = debtItem,
                DebtTypeList = await _db.DebtTypes
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Description
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: DebtItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DebtItemViewModel vm)
        {
            if (id != vm.DebtItem.Id) return NotFound();

            // Load a tracked entity
            var entity = await _db.DebtItems.FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null) return NotFound();

            // map allowed fields from vm.DebtItem -> entity
            entity.Name = vm.DebtItem.Name;
            entity.Balance = vm.DebtItem.Balance;
            entity.MinimumPayment = vm.DebtItem.MinimumPayment;
            entity.InterestRate = vm.DebtItem.InterestRate;
            entity.DebtTypeId = vm.DebtItem.DebtTypeId;
            entity.DueDate = 5;
            entity.SendDate = 25;

            // server-owned
            entity.LastUpdate = DateTime.UtcNow;

            // Clear & revalidate against the tracked entity (no prefix here)
            ModelState.Clear();
            if (!TryValidateModel(entity))
            {
                // rehydrate dropdowns etc.
                vm.DebtTypeList = await _db.DebtTypes
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Description
                    })
                    .ToListAsync();
                return View(vm);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                //_db.Update(debtItem);      // still a detached update; be careful with navs
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }


            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        // Re-populate dropdown if model state fails
        //ViewBag.Partners = await _db.Partners
        //        .Select(p => new SelectListItem
        //        {
        //            Value = p.Id.ToString(),
        //            Text = p.Name
        //        }).ToListAsync();

        //    return View(debtItem);
        //}


        // GET: DebtItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debtItem = await _db.DebtItems
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
            var debtItem = await _db.DebtItems.FindAsync(id);
            if (debtItem != null)
            {
                _db.DebtItems.Remove(debtItem);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DebtItemExists(int id)
        {
            return _db.DebtItems.Any(e => e.Id == id);
        }
    }
}
