using System;
using System.Collections.Generic;

namespace DebtSnowballApp.Models.Payoff
{
    public class PayoffResult
    {
        public string Strategy { get; set; } = "Snowball";
        public decimal ExtraPayment { get; set; }
        public List<PayoffMonth> Months { get; set; } = new();
        public decimal TotalInterest { get; set; }
        public int MonthCount => Months.Count;
    }

    public class PayoffMonth
    {
        public int MonthIndex { get; set; }                  // 1..N
        public DateTime MonthDate { get; set; }              // optional
        public List<PayoffLine> Lines { get; set; } = new(); // one per debt touched this month
    }

    public class PayoffLine
    {
        public int DebtId { get; set; }
        public string DebtName { get; set; } = string.Empty;
        public decimal Payment { get; set; }
        public decimal InterestPaid { get; set; }
        public decimal PrincipalPaid { get; set; }
        public decimal RemainingBalance { get; set; }
    }
}
