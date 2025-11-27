using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
//using DebtSnowballApp.ViewModels.QuickAnalysis;
using Microsoft.EntityFrameworkCore;

namespace DebtSnowballApp.Services
{
    public class QuickAnalysisCalculator : IQuickAnalysisCalculator
    {
        // Conservative default ROI for Wealth Builder
        private const decimal DefaultAnnualRoi = 0.05m; // 5%

        private readonly ApplicationDbContext _context;

        public QuickAnalysisCalculator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<QuickAnalysisResultViewModel> CalculateAsync(string ownerId)
        {
            // ----- Load data for this quick-analysis "owner" (user or temp) -----
            var person = await _context.QuickAnalysisPersonals
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == ownerId || p.TempUserId == ownerId);

            var debts = await _context.QaDebtItems
                .AsNoTracking()
                .Where(d => d.UserId == ownerId || d.TempUserId == ownerId)
                .ToListAsync();

            if (person == null || !debts.Any())
                throw new InvalidOperationException("QuickAnalysis requires personal info and at least one debt.");

            // Base numbers from QA debts
            var totalDebt = debts.Sum(d => d.Balance);
            var monthlyDebt = debts.Sum(d => d.MinimumPayment);

            // ----- CURRENT scenario: each debt paid with its own minimum (no snowball) -----
            ComputeCurrentScenario(debts, out var yearsCurrent, out var totalPaidCurrent);

            // ----- PLAN scenario: snowball using the same total monthly budget -----
            ComputeSnowballPlan(debts, monthlyDebt, out var yearsPlan, out var totalPaidPlan);

            // Guard against weird negatives
            if (yearsCurrent < 0) yearsCurrent = 0;
            if (yearsPlan < 0) yearsPlan = 0;

            var yearsSaved = yearsCurrent - yearsPlan;
            if (yearsSaved < 0) yearsSaved = 0;

            // ----- Wealth Builder: invest the current monthly debt payment for yearsSaved at ROI -----
            var wealth = ComputeWealth(monthlyDebt, yearsSaved, DefaultAnnualRoi);

            // ----- NEW Cost-Per-Day: wealth lost per day of delay -----
            decimal costPerDay = 0m;
            var daysSaved = yearsSaved * 365.0;
            if (daysSaved > 0)
            {
                costPerDay = decimal.Round(wealth / (decimal)daysSaved, 2);
            }

            // ----- Build result view model -----
            var vm = new QuickAnalysisResultViewModel
            {
                ClientName = BuildClientName(person),

                // Current
                CurrentTotalDebt = totalDebt,
                CurrentMonthlyDebt = monthlyDebt,
                CurrentDebtFreedomYears = yearsCurrent,
                CurrentPrincipalAndInterest = totalPaidCurrent,

                // Plan (snowball)
                PlanTotalDebt = totalDebt,              // principal same as current
                PlanMonthlyDebt = monthlyDebt,          // same budget, used smarter
                PlanDebtFreedomYears = yearsPlan,
                PlanPrincipalAndInterest = totalPaidPlan,

                // Cost of not taking action (wealth-based)
                CostPerDay = costPerDay,

                // Wealth builder block
                WealthBuilderTotalWealth = decimal.Round(wealth, 0),
                WealthBuilderMonthlyContribution = monthlyDebt,
                WealthBuilderPeriodYears = yearsSaved,
                WealthBuilderRoiPercent = (double)(DefaultAnnualRoi * 100m)
            };

            return vm;
        }

        // ---------- Helper: client name "First L." ----------

        private static string BuildClientName(QuickAnalysisPersonal person)
        {
            if (string.IsNullOrWhiteSpace(person.LastName))
                return person.FirstName;

            var initial = person.LastName[0];
            return $"{person.FirstName} {char.ToUpperInvariant(initial)}.";
        }

        // ---------- CURRENT: independent payoffs, no snowball ----------

        private static void ComputeCurrentScenario(
            List<QaDebtItem> debts,
            out double years,
            out decimal totalPaid)
        {
            double maxMonths = 0;
            decimal sumPaid = 0;

            foreach (var d in debts)
            {
                if (d.Balance <= 0 || d.MinimumPayment <= 0)
                    continue;

                var months = ComputeMonthsToPayoff(
                    (double)d.Balance,
                    (double)d.InterestRate,
                    (double)d.MinimumPayment);

                if (double.IsNaN(months) || double.IsInfinity(months))
                    months = 100 * 12; // cap at 100 years

                maxMonths = Math.Max(maxMonths, months);
                sumPaid += (decimal)months * d.MinimumPayment;
            }

            years = maxMonths / 12.0;
            totalPaid = sumPaid;
        }

        /// <summary>
        /// Number of months to pay off a loan with fixed payment and rate.
        /// </summary>
        private static double ComputeMonthsToPayoff(double balance, double annualRatePercent, double payment)
        {
            if (balance <= 0 || payment <= 0)
                return 0;

            var r = annualRatePercent / 100.0 / 12.0;

            if (r <= 0)
            {
                // No interest, simple division
                return balance / payment;
            }

            var interestOnly = balance * r;
            if (payment <= interestOnly)
            {
                // Payment too low to ever pay off – treat as very long
                return 100 * 12;
            }

            // n = ln(P / (P - rB)) / ln(1 + r)
            var numerator = Math.Log(payment / (payment - r * balance));
            var denominator = Math.Log(1 + r);
            var months = numerator / denominator;

            return months;
        }

        // ---------- PLAN: snowball with fixed total monthly budget ----------

        private class SnowballDebt
        {
            public decimal Balance;
            public decimal Rate;  // APR %
            public decimal Min;   // required minimum
        }

        private static void ComputeSnowballPlan(
            List<QaDebtItem> debts,
            decimal totalBudget,
            out double years,
            out decimal totalPaid)
        {
            var items = debts
                .Where(d => d.Balance > 0 && d.MinimumPayment > 0)
                .Select(d => new SnowballDebt
                {
                    Balance = d.Balance,
                    Rate = d.InterestRate,
                    Min = d.MinimumPayment
                })
                .ToList();

            if (!items.Any() || totalBudget <= 0)
            {
                years = 0;
                totalPaid = 0;
                return;
            }

            int month = 0;
            decimal paid = 0;
            const int maxMonths = 100 * 12; // safety cap at 100 years

            while (items.Any(i => i.Balance > 0.01m) && month < maxMonths)
            {
                month++;

                // 1) Accrue interest
                foreach (var i in items)
                {
                    if (i.Balance <= 0) continue;

                    var r = (double)i.Rate / 100.0 / 12.0;
                    if (r <= 0) continue;

                    i.Balance += i.Balance * (decimal)r;
                }

                decimal budget = totalBudget;

                // 2) Pay minimums
                foreach (var i in items.Where(x => x.Balance > 0.01m))
                {
                    if (budget <= 0) break;

                    var payment = Math.Min(i.Min, i.Balance);
                    if (payment > budget) payment = budget;

                    if (payment <= 0) continue;

                    i.Balance -= payment;
                    budget -= payment;
                    paid += payment;
                }

                // 3) Snowball leftover to smallest remaining balance
                if (budget > 0)
                {
                    var target = items
                        .Where(x => x.Balance > 0.01m)
                        .OrderBy(x => x.Balance) // classic snowball: smallest balance first
                        .FirstOrDefault();

                    if (target != null)
                    {
                        var extra = Math.Min(budget, target.Balance);
                        target.Balance -= extra;
                        budget -= extra;
                        paid += extra;
                    }
                }
            }

            years = month / 12.0;
            totalPaid = paid;
        }

        // ---------- Wealth Builder: invest monthlyContribution for years at annualRoi ----------

        private static decimal ComputeWealth(
            decimal monthlyContribution,
            double years,
            decimal annualRoi)
        {
            if (monthlyContribution <= 0 || years <= 0 || annualRoi <= 0)
                return 0;

            var r = (double)annualRoi / 12.0;      // monthly rate
            var n = (int)Math.Round(years * 12);   // months
            var pmt = (double)monthlyContribution;

            // FV of an ordinary annuity: FV = P * ((1+r)^n - 1) / r
            var fv = pmt * (Math.Pow(1 + r, n) - 1) / r;

            return (decimal)fv;
        }
    }
}
