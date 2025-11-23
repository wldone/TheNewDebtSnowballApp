using System;
using System.Collections.Generic;
using System.Linq;
using DebtSnowballApp.Models;

namespace DebtSnowballApp.Services
{
    public static class QuickEstimator
    {
        // Returns (months, totalInterest)
        // Strategy: "Snowball" (smallest balance first) or "Avalanche" (highest APR first)
        public static (int months, decimal interest) Estimate(IEnumerable<QaDebtItem> debts, decimal extra, string strategy)
        {
            var work = debts
                .Select(d => new Node
                {
                    Id = d.Id,
                    Name = d.Name ?? "",
                    Bal = Math.Max(0, d.Balance),
                    Rate = Math.Max(0, d.InterestRate),
                    Min = Math.Max(0, d.MinimumPayment)
                })
                .Where(n => n.Bal > 0.01m)
                .ToList();

            if (!work.Any()) return (0, 0m);

            int months = 0;
            decimal totalInterest = 0m;
            const int MAX = 600;

            while (work.Any(n => n.Bal > 0.01m) && months < MAX)
            {
                months++;

                // order by strategy
                IEnumerable<Node> ordered = (strategy ?? "Snowball").ToLowerInvariant() switch
                {
                    "avalanche" => work.OrderByDescending(n => n.Rate).ThenBy(n => n.Bal),
                    _ => work.OrderBy(n => n.Bal).ThenByDescending(n => n.Rate)
                };

                decimal available = work.Sum(n => n.Min) + Math.Max(0, extra);

                foreach (var n in ordered)
                {
                    if (available <= 0 || n.Bal <= 0.01m) continue;

                    decimal monthlyRate = n.Rate / 100m / 12m;
                    decimal interest = Math.Round(n.Bal * monthlyRate, 2, MidpointRounding.AwayFromZero);
                    decimal due = Math.Min(n.Bal + interest, n.Min);

                    decimal pay = Math.Min(available, n.Bal + interest);
                    if (pay < due) pay = Math.Min(available, due);
                    if (pay <= 0) continue;

                    decimal interestPaid = Math.Min(pay, interest);
                    decimal principal = pay - interestPaid;

                    n.Bal = Math.Max(0, n.Bal - principal);
                    available -= pay;

                    totalInterest += interestPaid;
                }

                work = work.Where(n => n.Bal > 0.01m).ToList();
                if (work.Count == 0) break;
                if (work.Sum(n => n.Min) + extra <= 0 && months > 1) break; // stall guard
            }

            return (months, Math.Round(totalInterest, 2, MidpointRounding.AwayFromZero));
        }

        private sealed class Node
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Bal { get; set; }
            public decimal Rate { get; set; } // APR %
            public decimal Min { get; set; }
        }
    }
}
