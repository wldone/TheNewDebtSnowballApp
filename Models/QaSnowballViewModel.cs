using DebtSnowballApp.Models.Payoff;

namespace DebtSnowballApp.Models
{
    public class QaSnowballViewModel
    {
        public string Strategy { get; set; } = "Snowball";
        public decimal ExtraPayment { get; set; }
        public IEnumerable<QaDebtItem> Debts { get; set; } = new List<QaDebtItem>();
        public List<PayoffMonth> PayoffPlan { get; set; } = new();
    }
}
