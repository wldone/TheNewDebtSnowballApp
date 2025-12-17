using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DebtSnowballApp.Data;
using DebtSnowballApp.Models;

namespace DebtSnowballApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        // 👇 THIS is the Input property you must have defined at class level
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "First name")]
            public string? FirstName { get; set; }

            [Display(Name = "Last name")]
            public string? LastName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // GET: /Identity/Account/Register?returnUrl=...&email=...
        public async Task OnGetAsync(string? returnUrl = null, string? email = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // ensure Input is not null
            Input ??= new InputModel();

            // 1️⃣ Prefill email from query string (Step4ThankYou or Login → Register)
            if (!string.IsNullOrEmpty(email))
            {
                Input.Email = email;
            }

            // 2️⃣ Prefill first/last from SESSION if available
            //    (replace keys if yours are different)
            var sessionFirstName = HttpContext.Session.GetString("QA_FirstName");
            var sessionLastName = HttpContext.Session.GetString("QA_LastName");

            if (string.IsNullOrEmpty(Input.FirstName) && !string.IsNullOrEmpty(sessionFirstName))
            {
                Input.FirstName = sessionFirstName;
            }

            if (string.IsNullOrEmpty(Input.LastName) && !string.IsNullOrEmpty(sessionLastName))
            {
                Input.LastName = sessionLastName;
            }

            // 3️⃣ Prefill from Quick Analysis personal info (via TempUserId) as an additional fallback
            var tempUserId = HttpContext.Session.GetString("TempUserId");
            if (!string.IsNullOrEmpty(tempUserId))
            {
                var qaPerson = await _context.QuickAnalysisPersonals
                    .FirstOrDefaultAsync(p => p.TempUserId == tempUserId);

                if (qaPerson != null)
                {
                    if (string.IsNullOrEmpty(Input.Email) && !string.IsNullOrEmpty(qaPerson.Email))
                    {
                        Input.Email = qaPerson.Email;
                    }

                    if (string.IsNullOrEmpty(Input.FirstName))
                        Input.FirstName = qaPerson.FirstName;
                     
                    if (string.IsNullOrEmpty(Input.LastName))
                        Input.LastName = qaPerson.LastName;
                }
            }
        }


        // POST: /Identity/Account/Register
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                // assumes ApplicationUser has these props
                FirstName = Input.FirstName,
                LastName = Input.LastName
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                user.LastLoginUtc = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // 🔁 Transfer QA data (personal + debts) from TempUserId → this new user
                var tempUserId = HttpContext.Session.GetString("TempUserId");
                if (!string.IsNullOrEmpty(tempUserId))
                {
                    await TransferQuickAnalysisDataAsync(tempUserId, user.Id);
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction(
                        actionName: "Edit",
                        controllerName: "Profile",
                        routeValues: new { area = "" }
                    );
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Something failed; redisplay form
            return Page();
        }

        // 🔧 Helper: move QA personal + debts to real user
        private async Task TransferQuickAnalysisDataAsync(string tempUserId, string userId)
        {
            var now = DateTime.UtcNow;

            // 1. Attach QuickAnalysisPersonal to real user
            var qaPerson = await _context.QuickAnalysisPersonals
                .FirstOrDefaultAsync(p => p.TempUserId == tempUserId);

            if (qaPerson != null)
            {
                qaPerson.UserId = userId;
                qaPerson.TempUserId = null;
            }

            // 2. Load QA debts
            var qaDebts = await _context.QaDebtItems
                .Where(d => d.TempUserId == tempUserId)
                .ToListAsync();

            if (!qaDebts.Any())
            {
                await _context.SaveChangesAsync();
                return;
            }

            // 3. Load DebtTypes so we can map by name/description
            var debtTypes = await _context.DebtTypes.ToListAsync();

            // Try to find an "Other" type as a safe default
            var defaultDebtType = debtTypes
                .FirstOrDefault(dt => dt.Description == "Other")
                ?? debtTypes.First();   // if no "Other", just use the first one

            int MapDebtTypeId(string qaName)
            {
                if (!debtTypes.Any())
                    return 0; // should never happen if you have seed data

                var name = (qaName ?? "").Trim().ToLowerInvariant();

                // Normalize QA names to your DebtType descriptions
                string target = name switch
                {
                    "mortgage" => "Mortgage",
                    "automobile" => "Auto Loan",      // or whatever your description is
                    "auto loan" => "Auto Loan",
                    "credit card" => "Credit Card",
                    "student loan" => "Student Loan",
                    "personal loan" => "Personal Loan",
                    "other" => "Other",
                    _ => "Other"
                };

                var match = debtTypes
                    .FirstOrDefault(dt => dt.Description.ToLower() == target.ToLower());

                return (match ?? defaultDebtType).Id;
            }

            // 4. Create real DebtItems from QA debts
            var newDebts = qaDebts.Select(d => new DebtItem
            {
                Name = d.Name,
                BegBalance = d.Balance,
                Balance = d.Balance,
                MinimumPayment = d.MinimumPayment,
                InterestRate = d.InterestRate,
                UserId = userId,
                CreationDate = now,
                LastUpdate = now,
                DebtTypeId = MapDebtTypeId(d.Name)  // 👈 key line: set valid FK
            }).ToList();

            _context.DebtItems.AddRange(newDebts);

            await _context.SaveChangesAsync();
        }

    }
}
