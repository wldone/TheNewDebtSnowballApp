using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using DebtSnowballApp.Areas.Admin.ViewModels; // to reuse UserEditVm
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtSnowballApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // GET: /Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // not logged in

            // map ApplicationUser -> UserEditVm (we'll ignore admin-only bits)
            var vm = new UserEditVm
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                PartnerId = user.PartnerId,

                FirstName = user.FirstName,
                LastName = user.LastName,
                Address1 = user.Address1,
                Address2 = user.Address2,
                City = user.City,
                State = user.State,
                PostalCode = user.PostalCode,
                Country = user.Country,

                PreferredStrategy = user.PreferredStrategy,
                PreferredMonthlyBudget = user.PreferredMonthlyBudget,

                // these are admin-ish; we can hide them in the view
                Role = null,
                Locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
            };

            // if you want partner dropdown for the user, you can reuse your helper
            vm.Partners = await _db.Partners
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = p.Name,
                    Value = p.Id.ToString(),
                    Selected = (user.PartnerId.HasValue && p.Id == user.PartnerId.Value)
                })
                .ToListAsync();

            return View(vm); // Views/Profile/Edit.cshtml
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditVm vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Ensure the posted Id matches the currently logged in user
            if (vm.Id != user.Id)
                return Forbid(); // prevent tampering

            if (!ModelState.IsValid)
            {
                vm.Partners = await _db.Partners
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Text = p.Name,
                        Value = p.Id.ToString(),
                        Selected = (user.PartnerId.HasValue && p.Id == user.PartnerId.Value)
                    })
                    .ToListAsync();

                return View(vm);
            }

            // allowed fields for self-edit
            user.Email = vm.Email;
            user.UserName = vm.UserName;
            user.PartnerId = vm.PartnerId;

            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.Address1 = vm.Address1;
            user.Address2 = vm.Address2;
            user.City = vm.City;
            user.State = vm.State;
            user.PostalCode = vm.PostalCode;
            user.Country = vm.Country;

            user.PreferredStrategy = vm.PreferredStrategy;
            user.PreferredMonthlyBudget = vm.PreferredMonthlyBudget;

            // DO NOT touch Lockout, roles, etc. here – those belong to Admin

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                vm.Partners = await _db.Partners
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Text = p.Name,
                        Value = p.Id.ToString(),
                        Selected = (user.PartnerId.HasValue && p.Id == user.PartnerId.Value)
                    })
                    .ToListAsync();

                return View(vm);
            }

            TempData["Toast"] = "Profile updated.";
            // send them somewhere – dashboard, debts list, etc.
            return RedirectToAction("Index", "Home");
        }
    }
}
