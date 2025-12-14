using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DebtSnowballApp.Models; // ApplicationUser

namespace DebtSnowballApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Email or username")]
            public string LoginInput { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null, string? email = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            // Clear any existing external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Populate for the view (buttons for Google/Microsoft, etc.)
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Optional: prefill login box if coming from somewhere with ?email=...
            if (!string.IsNullOrEmpty(email))
            {
                Input ??= new InputModel();
                Input.LoginInput = email;
            }
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                // repopulate on validation error
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                return Page();
            }

            // Allow login by username OR email
            var user = await _userManager.FindByNameAsync(Input.LoginInput)
                       ?? await _userManager.FindByEmailAsync(Input.LoginInput);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                user.LastLoginUtc = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                return LocalRedirect(ReturnUrl!);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl, RememberMe = Input.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                ModelState.AddModelError(string.Empty, "Account locked out.");
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }
    }
}
