namespace DebtSnowballApp.Models
{
    public class SnowballDebtState
    {
        public DebtItem Debt { get; set; } = default!;
        public decimal Remaining { get; set; }
        public bool Paid { get; set; } = false;
        public List<AmortizationRow> Schedule { get; set; } = new();
        public int Months { get; set; }
        public decimal InterestPaid { get; set; }
    }
}
