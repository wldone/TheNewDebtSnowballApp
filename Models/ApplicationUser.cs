using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DebtSnowballApp.Models
{
    public class ApplicationUser : IdentityUser // string key (GUID)
    {
        public int? PartnerId { get; set; }
        public Partner? Partner { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginUtc { get; set; }
        public ICollection<DebtItem> DebtItems { get; set; } = new List<DebtItem>();
        public PayoffStrategy PreferredStrategy { get; set; } = PayoffStrategy.Snowball;

        [Precision(18, 2)]
        public decimal PreferredMonthlyBudget { get; set; } = 0m;

        // --- Profile fields ---
        [MaxLength(50)] public string? FirstName { get; set; }
        [MaxLength(50)] public string? LastName { get; set; }

        [MaxLength(100)] public string? Address1 { get; set; }
        [MaxLength(100)] public string? Address2 { get; set; }
        [MaxLength(60)] public string? City { get; set; }
        [MaxLength(30)] public string? State { get; set; }
        [MaxLength(20)] public string? PostalCode { get; set; }
        [MaxLength(60)] public string? Country { get; set; }

        [NotMapped]
        public string FullName =>
            string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        [NotMapped]
        public string AddressOneLine =>
            string.Join(", ",
                new[]
                {
                    Address1,
                    Address2,
                    string.Join(" ", new[] { City, State }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                    PostalCode,
                    Country
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
