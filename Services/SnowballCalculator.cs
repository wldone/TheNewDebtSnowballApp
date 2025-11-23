using DebtSnowballApp.Models;

namespace DebtSnowballApp.Services
{
    public class SnowballCalculator
    {

        public List<SnowballResult> Calculate(List<DebtItem> debts, decimal monthlyBudget, PayoffStrategy strategy = PayoffStrategy.Snowball)
        {
            var results = new List<SnowballResult>();
            if (debts == null || !debts.Any()) return results;

            // Order the debts
            var orderedDebts = strategy switch
            {
                PayoffStrategy.Avalanche => debts.OrderByDescending(d => d.InterestRate).ToList(),
                _ => debts.OrderBy(d => d.Balance).ToList(), // Snowball default
            };

            // Track state per debt
            var debtStates = orderedDebts.Select(d => new SnowballDebtState
            {
                Debt = d,
                Remaining = d.Balance
            }).ToList();

            decimal baseMonthlyBudget = debts.Sum(d => d.MinimumPayment); // fixed base budget
            decimal snowballExtra = 0m;
            int month = 1;

            while (debtStates.Any(d => !d.Paid))
            {
                var remainingBudget = baseMonthlyBudget + snowballExtra;
                var targetDebt = debtStates.First(d => !d.Paid);

                foreach (var d in debtStates.Where(d => !d.Paid))
                {
                    decimal monthlyRate = d.Debt.InterestRate / 100m / 12m;
                    decimal interest = d.Remaining * monthlyRate;
                    decimal totalDue = d.Remaining + interest;

                    decimal basePayment = Math.Min(d.Debt.MinimumPayment, totalDue);
                    decimal extraPayment = (d == targetDebt) ? Math.Min(remainingBudget - baseMonthlyBudget, totalDue - basePayment) : 0;
                    decimal payment = basePayment + extraPayment;

                    decimal principal = payment - interest;
                    decimal endingBalance = d.Remaining + interest - payment;

                    d.Remaining += interest;
                    d.Remaining -= payment;
                    d.InterestPaid += interest;
                    d.Months++;

                    d.Schedule.Add(new AmortizationRow
                    {
                        Month = month,
                        StartingBalance = Math.Round(d.Remaining + payment - interest, 2),
                        Payment = Math.Round(payment, 2),
                        Interest = Math.Round(interest, 2),
                        EndingBalance = Math.Round(endingBalance, 2),
                        IsTarget = (d == targetDebt) // ✅ only mark true for the target debt
                    });

                    remainingBudget -= payment;

                    if (d.Remaining <= 0.01m)
                    {
                        d.Paid = true;
                        snowballExtra += d.Debt.MinimumPayment; // roll minimum payment into snowball
                    }
                }

                month++;
            }

            foreach (var d in debtStates)
            {
                results.Add(new SnowballResult
                {
                    Name = d.Debt.Name,
                    StartingBalance = d.Debt.Balance,
                    InterestRate = d.Debt.InterestRate,
                    MinimumPayment = d.Debt.MinimumPayment,
                    MonthsToPayoff = d.Months,
                    TotalInterestPaid = Math.Round(d.InterestPaid, 2),
                    AmortizationSchedule = d.Schedule
                });
            }

            return results;
        }


    }
}
