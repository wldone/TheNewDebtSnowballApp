using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DebtSnowballApp.Areas.Admin.ViewModels
{
    public class UserCreateVm
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Partner")]
        public int? PartnerId { get; set; }
        public List<SelectListItem> Partners { get; set; } = new();

        // Optional role selection
        public string? Role { get; set; }
        public List<SelectListItem> Roles { get; set; } = new();

        // Profile fields
        [MaxLength(50)] public string? FirstName { get; set; }
        [MaxLength(50)] public string? LastName { get; set; }

        [MaxLength(100)] public string? Address1 { get; set; }
        [MaxLength(100)] public string? Address2 { get; set; }
        [MaxLength(60)] public string? City { get; set; }
        [MaxLength(30)] public string? State { get; set; }
        [MaxLength(20)] public string? PostalCode { get; set; }
        [MaxLength(60)] public string? Country { get; set; }
    }
}
