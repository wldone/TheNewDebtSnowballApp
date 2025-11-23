using System.ComponentModel.DataAnnotations;

namespace DebtSnowballApp.Models
{
    public class Partner
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;
        [StringLength(50)]
        public string? Code { get; set; }
        [EmailAddress, StringLength(200)]
        public string? SupportEmail { get; set; }
        public int SortOrder { get; set; } = 0;    // ← add

        [Phone, StringLength(30)]
        public string? SupportPhone { get; set; }

        [Url, StringLength(300)]
        public string? Website { get; set; }

        public bool Active { get; set; } = true;

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Optional optimistic concurrency
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    }
}
