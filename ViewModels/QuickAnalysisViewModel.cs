using System.Collections.Generic;
using System.Linq;

namespace DebtSnowballApp.Models.ViewModels
{
    public class QuickAnalysisViewModel
    {
        // Inputs/choices
        public string Strategy { get; set; } = "Snowball"; // "Snowball" | "Avalanche"
        public decimal ExtraPayment { get; set; }

        // Data to display
        public IEnumerable<QaDebtItem> Debts { get; set; } = new List<QaDebtItem>();

        // Summary metrics
        public decimal TotalBalance => Debts.Sum(d => d.Balance);
        public decimal TotalMinPayments => Debts.Sum(d => d.MinimumPayment);

        // Weighted APR (balance-weighted) just for display context
        public decimal WeightedAprPercent => TotalBalance <= 0 ? 0 :
            Debts.Sum(d => d.Balance * d.InterestRate) / TotalBalance;

        // Quick estimates (months & interest) – computed in controller
        public int? EstMonthsSnowball { get; set; }
        public decimal? EstInterestSnowball { get; set; }
        public int? EstMonthsAvalanche { get; set; }
        public decimal? EstInterestAvalanche { get; set; }

        // Helper flags
        public bool IsDemo { get; set; }
    }
}
