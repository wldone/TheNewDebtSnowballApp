using System.Data;
using DebtSnowballApp.Areas.Admin.ViewModels;
using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DebtSnowballApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }
        // ======== INDEX ========

        [HttpGet]
        public async Task<IActionResult> Index(string? sort = "name", string? dir = "asc", string? q = null, string? role = null)
        {
            sort = (sort ?? "name").ToLowerInvariant();
            dir = (dir ?? "asc").ToLowerInvariant();


            var users = _userManager.Users
                .Include(u => u.Partner)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                users = users.Where(u =>
                    (u.Email != null && u.Email.Contains(term)) ||
                    (u.UserName != null && u.UserName.Contains(term)) ||
                    (u.FirstName != null && u.FirstName.Contains(term)) ||
                    (u.LastName != null && u.LastName.Contains(term)) ||
                    (u.Address1 != null && u.Address1.Contains(term)) ||
                    (u.Address2 != null && u.Address2.Contains(term)) ||
                    (u.City != null && u.City.Contains(term)) ||
                    (u.State != null && u.State.Contains(term)) ||
                    (u.PostalCode != null && u.PostalCode.Contains(term)) ||
                    (u.Country != null && u.Country.Contains(term)) ||
                    (u.Partner != null && u.Partner.Name.Contains(term)));
            }
            // 🔎 Role filter (server-side)
            if (!string.IsNullOrWhiteSpace(role))
            {
                var userIdsWithRole =
                    from ur in _db.UserRoles
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where r.Name == role
                    select ur.UserId;

                users = users.Where(u => userIdsWithRole.Contains(u.Id));
            }

            // Sort
            users = (sort, dir) switch
            {
                ("name", "desc") => users.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName),
                ("name", _) => users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),

                ("email", "desc") => users.OrderByDescending(u => u.Email),
                ("email", _) => users.OrderBy(u => u.Email),

                ("username", "desc") => users.OrderByDescending(u => u.UserName),
                ("username", _) => users.OrderBy(u => u.UserName),

                ("partner", "desc") => users.OrderByDescending(u => u.Partner!.Name ?? ""),
                ("partner", _) => users.OrderBy(u => u.Partner!.Name ?? ""),


                ("city", "desc") => users.OrderByDescending(u => u.City).ThenByDescending(u => u.State),
                ("city", _) => users.OrderBy(u => u.City).ThenBy(u => u.State),

                ("state", "desc") => users.OrderByDescending(u => u.State).ThenByDescending(u => u.City),
                ("state", _) => users.OrderBy(u => u.State).ThenBy(u => u.City),

                ("zip", "desc") => users.OrderByDescending(u => u.PostalCode),
                ("zip", _) => users.OrderBy(u => u.PostalCode),
                   ("lastlogin", "desc") => users.OrderByDescending(u => u.LastLoginUtc),
                ("lastlogin", _) => users.OrderBy(u => u.LastLoginUtc),


                _ => users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            };

            ViewBag.Sort = sort;
            ViewBag.Dir = dir;
            ViewBag.Query = q;
            ViewBag.Role = role;

            // All roles for dropdown
            ViewBag.AllRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            var list = await users.ToListAsync();

            // Roles lookup for badges (view-only)
            var userIds = list.Select(u => u.Id).ToList();
            var rolePairs = await (from ur in _db.UserRoles
                                   join r in _db.Roles on ur.RoleId equals r.Id
                                   where userIds.Contains(ur.UserId)
                                   select new { ur.UserId, RoleName = r.Name })
                                  .ToListAsync();
            var rolesByUser = rolePairs
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName!)
                                                .Where(n => n != null)
                                                .OrderBy(n => n)
                                                .ToList());
            ViewBag.RolesByUser = rolesByUser;
            ViewData["Toolbar"] = new AdminListToolbarModel
            {
                Title = "Manage Users",
                CreateText = "Create User",
                CreateUrl = Url.Action("Create", "Users", new { area = "Admin" })!,
                SearchPlaceholder = "Search users…",
                QueryParamName = "q",
                CurrentQuery = q ?? ""
            };

            //var list = await users.ToListAsync();

            // Build a roles lookup for the users in this page
            //var userIds = list.Select(u => u.Id).ToList();

            //var rolePairs = await (from ur in _db.UserRoles
            //                       join r in _db.Roles on ur.RoleId equals r.Id
            //                       where userIds.Contains(ur.UserId)
            //                       select new { ur.UserId, RoleName = r.Name })
            //                      .ToListAsync();

            //var rolesByUser = rolePairs
            //    .GroupBy(x => x.UserId)
            //    .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName!)
            //                                    .Where(n => n != null)
            //                                    .OrderBy(n => n)
            //                                    .ToList());

            // pass to view
            ViewBag.RolesByUser = rolesByUser;


            return View(list); // renders Areas/Admin/Views/Users/Index.cshtml
        }

        // ======== CREATE ========

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new UserCreateVm();
            await PopulateLookupsAsync(vm.Partners, vm.Roles);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(vm.Partners, vm.Roles);
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,    // keep username = email (simple)
                Email = vm.Email,
                PartnerId = vm.PartnerId,

                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Address1 = vm.Address1,
                Address2 = vm.Address2,
                City = vm.City,
                State = vm.State,
                PostalCode = vm.PostalCode,
                Country = vm.Country
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                await PopulateLookupsAsync(vm.Partners, vm.Roles);
                return View(vm);
            }

            // Optional: assign role
            if (!string.IsNullOrWhiteSpace(vm.Role))
            {
                if (await _roleManager.RoleExistsAsync(vm.Role))
                    await _userManager.AddToRoleAsync(user, vm.Role);
                else
                    ModelState.AddModelError(nameof(vm.Role), "Selected role does not exist.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(vm.Partners, vm.Roles);
                return View(vm);
            }

            TempData["Toast"] = "User created.";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Partner)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var vm = new AdminUserDetailsVm
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PartnerId = user.PartnerId,
                PartnerName = user.Partner?.Name,
                Partners = await _db.Partners
                    .OrderBy(p => p.Name)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                    .ToListAsync(),
                Roles = roles.ToList(),
                AllRoles = _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new SelectListItem { Value = r.Name!, Text = r.Name! })
                    .ToList(),
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                CreatedUtc = user.CreatedUtc,
                LastLoginUtc = user.LastLoginUtc
            };

            return View(vm);
        }

        // ======== Profile ========

        [HttpGet]
        public async Task<IActionResult> ProfileCard(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var u = await _userManager.Users
                .Include(x => x.Partner)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (u == null) return NotFound();

            var vm = new UserProfileVm
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Address1 = u.Address1,
                Address2 = u.Address2,
                City = u.City,
                State = u.State,
                PostalCode = u.PostalCode,
                Country = u.Country,
                PartnerName = u.Partner?.Name,
                Locked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                PreferredStrategy = u.PreferredStrategy,
                PreferredMonthlyBudget = u.PreferredMonthlyBudget
            };

            return PartialView("_UserProfileCard", vm);
        }

        // ======== EDIT ========

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Partner)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault();

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

                Role = currentRole,

                // 👇 locked if lockout end is in the future
                Locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
            };

            await PopulateLookupsAsync(vm.Partners, vm.Roles, currentRole, user.PartnerId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(vm.Partners, vm.Roles, vm.Role, vm.PartnerId);
                return View(vm);
            }

            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            // basic fields
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

            // 👇 lock/unlock handling
            if (vm.Locked)
            {
                // lock for a long period (adjust as you like)
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            }
            else
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0; // optional: clear failures
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                await PopulateLookupsAsync(vm.Partners, vm.Roles, vm.Role, vm.PartnerId);
                return View(vm);
            }

            // role update (single-role approach)
            var currentRoles = await _userManager.GetRolesAsync(user);
            if ((vm.Role ?? "") != (currentRoles.FirstOrDefault() ?? ""))
            {
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!string.IsNullOrWhiteSpace(vm.Role) && await _roleManager.RoleExistsAsync(vm.Role))
                    await _userManager.AddToRoleAsync(user, vm.Role);
            }

            TempData["Toast"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }

        // ======== UPDATE ========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(AdminUserDetailsVm vm)
        {
            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            // Editable fields
            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.PartnerId = vm.PartnerId;
            user.EmailConfirmed = vm.EmailConfirmed;
            user.TwoFactorEnabled = vm.TwoFactorEnabled;
            user.LockoutEnabled = vm.LockoutEnabled;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return await Details(vm.Id); // reload with errors
            }

            TempData["Toast"] = "User updated.";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        // ======== ROLES ========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["Err"] = $"Role '{role}' does not exist.";
                return RedirectToAction(nameof(Details), new { id });
            }
            await _userManager.AddToRoleAsync(user, role);
            TempData["Toast"] = $"Added role '{role}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userManager.RemoveFromRoleAsync(user, role);
            TempData["Toast"] = $"Removed role '{role}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ======== UNLOCK ========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["Toast"] = "User unlocked.";
            return RedirectToAction(nameof(Details), new { id });
        }
        
        // ======== Helpers ========

        private async Task PopulateLookupsAsync(
            List<SelectListItem> partnerItems,
            List<SelectListItem> roleItems,
            string? selectedRole = null,
            int? selectedPartnerId = null)
        {
            partnerItems.Clear();
            var partners = await _db.Partners
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            partnerItems.Add(new SelectListItem { Text = "(none)", Value = "" });
            partnerItems.AddRange(partners.Select(p => new SelectListItem
            {
                Text = p.Name,
                Value = p.Id.ToString(),
                Selected = (selectedPartnerId.HasValue && p.Id == selectedPartnerId.Value)
            }));

            roleItems.Clear();
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            roleItems.Add(new SelectListItem { Text = "(none)", Value = "" });
            roleItems.AddRange(roles.Select(r => new SelectListItem
            {
                Text = r.Name!,
                Value = r.Name!,
                Selected = (!string.IsNullOrWhiteSpace(selectedRole) && r.Name == selectedRole)
            }));
        }
    }
}
