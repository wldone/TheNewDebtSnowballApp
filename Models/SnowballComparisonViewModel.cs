namespace DebtSnowballApp.Models
{
    public class SnowballComparisonViewModel
    {
        public decimal MonthlyBudget { get; set; }
        public List<SnowballResult> Snowball { get; set; } = new();
        public List<SnowballResult> Avalanche { get; set; } = new();

        public decimal SnowballTotalInterest => Snowball.Sum(d => d.TotalInterestPaid);
        public decimal AvalancheTotalInterest => Avalanche.Sum(d => d.TotalInterestPaid);

        public int SnowballTotalMonths => Snowball.Max(d => d.MonthsToPayoff);
        public int AvalancheTotalMonths => Avalanche.Max(d => d.MonthsToPayoff);
    }


}
