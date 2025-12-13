namespace DebtSnowballApp.Models
{
    public class QuickAnalysisResultViewModel
    {
        // Header
        public string ClientName { get; set; } = string.Empty;
        public string? ClientEmail { get; set; }

        // LEFT COLUMN – Current Debt
        public decimal CurrentTotalDebt { get; set; }
        public decimal CurrentMonthlyDebt { get; set; }
        public double CurrentDebtFreedomYears { get; set; }
        public decimal CurrentPrincipalAndInterest { get; set; }

        // RIGHT COLUMN – “My Financial Outlook”
        public decimal PlanTotalDebt { get; set; }
        public decimal PlanMonthlyDebt { get; set; }
        public double PlanDebtFreedomYears { get; set; }
        public decimal PlanPrincipalAndInterest { get; set; }

        public decimal TotalSavings => CurrentPrincipalAndInterest - PlanPrincipalAndInterest;

        // Cost of Not Taking Action
        public decimal CostPerDay { get; set; }

        // Wealth Builder
        public decimal WealthBuilderTotalWealth { get; set; }  // e.g. 1_570_454
        public decimal WealthBuilderMonthlyContribution { get; set; } // 3_580
        public double WealthBuilderPeriodYears { get; set; }   // 20.8
        public double WealthBuilderRoiPercent { get; set; }    // 5
    }
}
