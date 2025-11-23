using System;
using System.Collections.Generic;
using System.Linq;
using DebtSnowballApp.Models.Interfaces;   // IDebtLike
using DebtSnowballApp.Models.Payoff;      // PayoffResult, PayoffMonth, PayoffLine
using DebtSnowballApp.Services.Interfaces; // IDebtPayoffCalculator<T>

namespace DebtSnowballApp.Services
{
    /// <summary>
    /// Generic payoff calculator that supports Snowball (default) and Avalanche strategies.
    /// Works for any debt entity that implements IDebtLike (e.g., DebtItem, QaDebtItem).
    /// </summary>
    public class DebtPayoffCalculator<TDebt> : IDebtPayoffCalculator<TDebt>
        where TDebt : IDebtLike
    {
        public PayoffResult Calculate(IEnumerable<TDebt> sourceDebts, decimal extraPayment, string strategy)
        {
            var result = new PayoffResult
            {
                Strategy = string.IsNullOrWhiteSpace(strategy) ? "Snowball" : strategy,
                ExtraPayment = extraPayment < 0 ? 0 : extraPayment
            };

            // Copy to a mutable working list and filter out paid-off items
            var debts = sourceDebts?
                .Select(d => new WorkingDebt
                {
                    Id = d.Id,
                    Name = d.Name ?? string.Empty,
                    Balance = Math.Max(0, d.Balance),
                    InterestRate = Math.Max(0, d.InterestRate),
                    MinimumPayment = Math.Max(0, d.MinimumPayment)
                })
                .Where(d => d.Balance > 0.01m)
                .ToList() ?? new List<WorkingDebt>();

            if (debts.Count == 0)
                return result;

            // Safety brake to prevent infinite loops (e.g., min payments == 0)
            const int maxMonths = 600;
            var monthIndex = 0;

            while (debts.Any(d => d.Balance > 0.01m) && monthIndex < maxMonths)
            {
                monthIndex++;
                var month = new PayoffMonth
                {
                    MonthIndex = monthIndex,
                    MonthDate = DateTime.Today.AddMonths(monthIndex - 1)
                };

                // Compute total available = sum of mins + extra
                decimal available = debts.Sum(d => d.MinimumPayment) + result.ExtraPayment;
                if (available < 0) available = 0;

                // Choose ordering by strategy
                IEnumerable<WorkingDebt> ordered = result.Strategy.ToLowerInvariant() switch
                {
                    "avalanche" => debts.OrderByDescending(d => d.InterestRate).ThenBy(d => d.Balance),
                    _ => debts.OrderBy(d => d.Balance).ThenByDescending(d => d.InterestRate) // Snowball default
                };

                foreach (var debt in ordered)
                {
                    if (available <= 0) break;
                    if (debt.Balance <= 0.01m) continue;

                    decimal monthlyRate = debt.InterestRate / 100m / 12m;
                    decimal interestAccrued = Round2(debt.Balance * monthlyRate);

                    // Minimum due cannot exceed payoff amount (balance + interest)
                    decimal minDueForDebt = Math.Min(debt.Balance + interestAccrued, debt.MinimumPayment);

                    // Pay at least min (if we have it), otherwise pay whatever remains
                    decimal desired = Math.Max(minDueForDebt, 0);
                    decimal pay = Math.Min(available, debt.Balance + interestAccrued);

                    // If we still have room beyond mins (snowball/extra), allocate it to this debt
                    if (pay < desired) pay = Math.Min(available, desired);

                    // If we still have more available after mins, push all remaining to current debt (primary target)
                    // Note: because we iterate in strategy order, this effectively snowballs/avalanches
                    // as extra flows to the first eligible debt in the ordering.
                    // 'pay' already caps at balance + interest; no overpay.

                    if (pay <= 0)
                        continue;

                    decimal interestPaid = Math.Min(pay, interestAccrued);
                    decimal principalPaid = pay - interestPaid;

                    debt.Balance = Math.Max(0, debt.Balance - principalPaid);
                    available -= pay;

                    month.Lines.Add(new PayoffLine
                    {
                        DebtId = debt.Id,
                        DebtName = debt.Name,
                        Payment = Round2(pay),
                        InterestPaid = Round2(interestPaid),
                        PrincipalPaid = Round2(principalPaid),
                        RemainingBalance = Round2(debt.Balance)
                    });

                    result.TotalInterest += Round2(interestPaid);
                }

                // Remove tiny residuals
                debts = debts.Where(d => d.Balance > 0.01m).ToList();

                if (!month.Lines.Any())
                    break; // nothing moved this month -> stop to avoid infinite loop

                result.Months.Add(month);
            }

            return result;
        }

        private static decimal Round2(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private sealed class WorkingDebt
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Balance { get; set; }
            public decimal InterestRate { get; set; }    // annual percent (e.g., 19.99)
            public decimal MinimumPayment { get; set; }  // monthly minimum
        }
    }
}
