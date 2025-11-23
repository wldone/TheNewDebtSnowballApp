using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using DebtSnowballApp.Models.ViewModels;
using DebtSnowballApp.Services;
using DebtSnowballApp.Services.Interfaces;       // IDebtPayoffCalculator
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtSnowballApp.Controllers
{
    // Remove class-level [Authorize] so other actions can choose their own policy
    public class QaSnowballController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDebtPayoffCalculator<QaDebtItem> _calculator;

        public QaSnowballController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IDebtPayoffCalculator<QaDebtItem> calculator)
        {
            _db = context;
            _userManager = userManager;
            _calculator = calculator;
        }
        // --- Quick Analysis (summary) ---
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Quick(string strategy = "Snowball", decimal extraPayment = 0)
        {
            var isAuthed = User?.Identity?.IsAuthenticated == true;
            List<QaDebtItem> qaDebts;

            if (isAuthed)
            {
                var userId = _userManager.GetUserId(User);
                qaDebts = await _db.QaDebtItems
                    .Where(d => d.UserId == userId)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
            }
            else
            {
                qaDebts = new()
                {
                    new QaDebtItem { Id = 1, Name = "Visa Demo",  Balance = 1200m, InterestRate = 19.99m, MinimumPayment = 35m },
                    new QaDebtItem { Id = 2, Name = "Store Card", Balance =  650m, InterestRate = 24.99m, MinimumPayment = 25m },
                    new QaDebtItem { Id = 3, Name = "Auto Loan",  Balance = 5400m, InterestRate =  6.50m, MinimumPayment = 180m }
                };
            }

            var vm = new QuickAnalysisViewModel
            {
                Strategy = strategy,
                ExtraPayment = extraPayment,
                Debts = qaDebts,
                IsDemo = !isAuthed
            };

            // Quick estimates for BOTH strategies so user can compare
            var snow = QuickEstimator.Estimate(qaDebts, extraPayment, "Snowball");
            var aval = QuickEstimator.Estimate(qaDebts, extraPayment, "Avalanche");
            vm.EstMonthsSnowball = snow.months;
            vm.EstInterestSnowball = snow.interest;
            vm.EstMonthsAvalanche = aval.months;
            vm.EstInterestAvalanche = aval.interest;

            return View(vm);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index(string strategy = "Snowball", decimal extraPayment = 0)
        {
            var isAuthed = User?.Identity?.IsAuthenticated == true;
            List<QaDebtItem> qaDebts;

            if (isAuthed)
            {
                var userId = _userManager.GetUserId(User);
                qaDebts = await _db.QaDebtItems
                    .Where(d => d.UserId == userId)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
            }
            else
            {
                qaDebts = new()
                {
                    new QaDebtItem { Id = 1, Name = "Visa Demo",  Balance = 1200m, InterestRate = 19.99m, MinimumPayment = 35m },
                    new QaDebtItem { Id = 2, Name = "Store Card", Balance =  650m, InterestRate = 24.99m, MinimumPayment = 25m },
                    new QaDebtItem { Id = 3, Name = "Auto Loan",  Balance = 5400m, InterestRate =  6.50m, MinimumPayment = 180m }
                };
            }

            var payoff = _calculator.Calculate(qaDebts, extraPayment, strategy);

            var vm = new QaSnowballViewModel
            {
                Strategy = strategy,
                ExtraPayment = extraPayment,
                Debts = qaDebts,
                PayoffPlan = payoff.Months
            };

            ViewBag.IsDemo = !isAuthed;
            return View(vm);
        }
    }
}
