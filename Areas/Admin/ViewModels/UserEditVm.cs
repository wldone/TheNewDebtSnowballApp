using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using DebtSnowballApp.Models; // for PayoffStrategy

namespace DebtSnowballApp.Areas.Admin.ViewModels
{
    public class UserEditVm
    {
        [Required] public string Id { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Partner")]
        public int? PartnerId { get; set; }
        public List<SelectListItem> Partners { get; set; } = new();

        // Optional role selection (single-role model)
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

        // Preferences
        [Display(Name = "Preferred Strategy")]
        public PayoffStrategy PreferredStrategy { get; set; }

        [Display(Name = "Preferred Monthly Budget")]
        [Range(0, 1_000_000)]
        [DataType(DataType.Currency)]
        public decimal PreferredMonthlyBudget { get; set; }

        // Lock/unlock
        [Display(Name = "Locked")]
        public bool Locked { get; set; }
    }
}
