using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using DebtSnowballApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[Authorize] // just needs to be logged in
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: /Profile/Edit
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge(); // not logged in (shouldn't happen with [Authorize])

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

            // self-service shouldn’t touch these
            Role = null,
            Locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
        };

        return View(vm);
    }

    // POST: /Profile/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // always load the current user from the ClaimsPrincipal, 
        // don't trust the Id coming from the form
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // optional: extra safety – if someone posts a different Id, reject
        if (user.Id != model.Id)
        {
            return Forbid();
        }

        // fields a normal user is allowed to change
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Address1 = model.Address1;
        user.Address2 = model.Address2;
        user.City = model.City;
        user.State = model.State;
        user.PostalCode = model.PostalCode;
        user.Country = model.Country;

        user.PreferredStrategy = model.PreferredStrategy;
        user.PreferredMonthlyBudget = model.PreferredMonthlyBudget;

        // Email / UserName: only if you want users to edit them
        user.Email = model.Email;
        user.UserName = model.UserName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        TempData["ProfileUpdated"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Edit));
    }
}
