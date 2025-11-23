using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DebtSnowballApp.Models.Interfaces;

namespace DebtSnowballApp.Models
{
    public partial class DebtItem : IDebtLike
    {
        public int Id { get; set; }

        // REQUIRED: belongs to a User (string key from Identity)
        [Required]
        [BindNever]
        public string UserId { get; set; } = string.Empty;

        [ValidateNever]                       // avoid binding nav props from requests
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = default!;

        // Core fields
        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;  // e.g., "Chase Visa"

        [Required, Precision(18, 2)]
        public decimal BegBalance { get; set; }

        [Required, Precision(18, 2)]
        public decimal Balance { get; set; }

        // Decide: store APR as 0.1999 (19.99%) or 19.99. Pick one and be consistent.
        // I’ll assume APR as percent (e.g., 19.99) since you’re using [Precision(18,2)] everywhere.
        [Required, Range(0, 100), Precision(18, 2)]
        public decimal InterestRate { get; set; }  // percent

        [Required, Precision(18, 2)]
        public decimal MinimumPayment { get; set; }

        // Optional modeling fields (keep if you use them; otherwise drop to simplify)
        [Precision(18, 2)] public decimal Rate { get; set; }       // clarify meaning or remove
        public int Term { get; set; }
        [Precision(18, 2)] public decimal Payment { get; set; }
        [Precision(18, 2)] public decimal MinPmtAmount { get; set; }
        [Precision(18, 2)] public decimal MinPmtPercent { get; set; }
        [Precision(18, 2)] public decimal Payment2 { get; set; }
        [Precision(18, 2)] public decimal Rate2 { get; set; }
        public int Term2 { get; set; }
        [Precision(18, 2)] public decimal Extra { get; set; }
        [Precision(18, 2)] public decimal Fees { get; set; }

        public byte Status { get; set; }
        public int Rank { get; set; }

        // Looks like day-of-month fields; enforce 1..31
        [Range(1, 31)]
        public int DueDate { get; set; } = 5;

        [Range(1, 31)]
        public int SendDate { get; set; } = 5;

        [Precision(18, 2)] public decimal YourBalance { get; set; }
        [Precision(18, 2)] public decimal YourPayment { get; set; }

        // Dates (make nullable if not always known; set defaults server-side)
        [DataType(DataType.Date)]
        public DateTime? OrigDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? OrigPayoffDate { get; set; }

        [Precision(18, 2)]
        public decimal OrigPayoffAmount { get; set; }

        [DataType(DataType.Date)]
        public DateTime? YourPayoffDate { get; set; }

        [Precision(18, 2)]
        public decimal YourPayoffAmount { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        // Consider string? if this refers to an Identity user id
        public string? ModifiedBy { get; set; }

        // DebtType FK
        [Range(1, int.MaxValue, ErrorMessage = "Please select a debt type.")]
        public int DebtTypeId { get; set; }

        [ValidateNever]
        [ForeignKey(nameof(DebtTypeId))]
        public DebtType? DebtType { get; set; }

        [NotMapped]
        public string? DebtTypeDesc => DebtType?.Description;
    }
}
