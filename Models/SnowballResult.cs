
namespace DebtSnowballApp.Models
{
    public class SnowballResult
    {
        public string Name { get; set; }
        public decimal StartingBalance { get; set; } // was BegBalance
        public decimal InterestRate { get; set; }
        public decimal MinimumPayment { get; set; }
        public int MonthsToPayoff { get; set; }
        public decimal TotalInterestPaid { get; set; }
        public decimal EndingBalance => 0m;
        public List<AmortizationRow> AmortizationSchedule { get; set; } = new();


    }
}
