using Microsoft.AspNetCore.Mvc.Rendering;

namespace DebtSnowballApp.Areas.Admin.ViewModels
{
    public class AdminUserDetailsVm
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string? UserName { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public int? PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public List<SelectListItem> Partners { get; set; } = new();

        public List<string> Roles { get; set; } = new();
        public List<SelectListItem> AllRoles { get; set; } = new();
        public string? RoleToAdd { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }

        public DateTimeOffset? CreatedUtc { get; set; }
        public DateTimeOffset? LastLoginUtc { get; set; }

        public bool IsLockedOut =>
            LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    }
}
