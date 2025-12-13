using System.ComponentModel.DataAnnotations;

namespace DebtSnowballApp.Models
{
    public class QuickAnalysisOrderViewModel
    {
        [Required]
        [Display(Name = "Name on Card")]
        public string NameOnCard { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Card Number")]
        [CreditCard]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Expiration Month")]
        public int ExpirationMonth { get; set; }

        [Required]
        [Display(Name = "Expiration Year")]
        public int ExpirationYear { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 3)]
        [Display(Name = "Security Code")]
        public string SecurityCode { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }
    }
}
