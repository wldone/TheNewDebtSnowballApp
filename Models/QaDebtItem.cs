using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using DebtSnowballApp.Models.Interfaces; 

namespace DebtSnowballApp.Models
{

    public partial class QaDebtItem : IDebtLike
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Precision(18, 2)]
        [Display(Name="Beginning Balance")]
        public decimal BegBalance { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1_000_000)]
        public decimal Balance { get; set; }

             [Required]
        [Precision(5, 2)]
        [Display(Name = "Interest Rate (%)")]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal InterestRate { get; set; }

     [Display(Name = "Minimum Payment")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1_000_000)]
        public decimal MinimumPayment { get; set; }

        
        [ValidateNever]
        public string? UserId { get; set; }

        [ValidateNever]
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        // Anonymous session id (no FK)
        public string? TempUserId { get; set; }

        //[Precision(18, 2)]
        //public decimal ExtraPayment { get; set; }

 //[Required]
        //[ValidateNever]
        //public int PartnerId { get; set; }

        //[ValidateNever]
        //[ForeignKey("PartnerId")]
        //public Partner? Partner { get; set; }

        //[Required]
        //public string Name { get; set; } = string.Empty;

        //[Required]
        //[Precision(18, 4)]
        //public decimal Balance { get; set; }


        //[Required]
        //[Precision(18, 2)]
        //public decimal Rate { get; set; }


        //[Display(Name = "Minimum Payment")]
        //[Column(TypeName = "decimal(18,2)")]
        //[Range(0, 1_000_000)]
        //public decimal MinimumPayment { get; set; }
        //[Precision(18, 2)]
        //public decimal ExtraPayment { get; set; }

        //public int Term { get; set; }


        //[DataType(DataType.Date)]
        //public DateTime CreationDate { get; set; }

        //[DataType(DataType.Date)]
        //public DateTime LastUpdate { get; set; }

        //public int DebtTypeId { get; set; }

    //    [ForeignKey("DebtTypeId")]
    //    public DebtType? DebtType { get; set; }

    //    [NotMapped]
    //    public string? DebtTypeDesc => DebtType?.Description;
    }
}
