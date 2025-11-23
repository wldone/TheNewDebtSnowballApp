using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using DebtSnowballApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtSnowballApp.Controllers
{
    [Authorize]
    public class SnowballController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SnowballCalculator _calculator;

        public SnowballController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SnowballCalculator calculator)
        {
            _context = context;
            _userManager = userManager;
            _calculator = calculator;
        }

        public async Task<IActionResult> Index(decimal? monthlyBudget, PayoffStrategy? strategy)
        {
            var user = await _userManager.GetUserAsync(User);

            var debts = await _context.DebtItems
                .Where(d => d.UserId == user.Id && d.Balance > 0)
                .ToListAsync();

            if (!debts.Any())
            {
                ViewBag.Message = "No active debts to simulate.";
                return View(new List<SnowballResult>());
            }

            // Load from user preferences if not explicitly passed
            var minRequired = debts.Sum(d => d.MinimumPayment);
            decimal budget = Math.Max(monthlyBudget ?? user.PreferredMonthlyBudget, minRequired);
            var selectedStrategy = strategy ?? user.PreferredStrategy;

            //var results = _calculator.Calculate(debts, budget, selectedStrategy);
            var snowballResults = _calculator.Calculate(debts, budget, selectedStrategy);
            ViewBag.MonthlyBudget = budget;
            ViewBag.Strategy = selectedStrategy;

            return View(snowballResults);
        }
        public async Task<IActionResult> Compare(decimal? monthlyBudget)
        {
            var user = await _userManager.GetUserAsync(User);

            var debts = await _context.DebtItems
                .Where(d => d.UserId == user.Id && d.Balance > 0)
                .ToListAsync();

            if (!debts.Any())
            {
                ViewBag.Message = "No active debts to simulate.";
                return View(new SnowballComparisonViewModel());
            }

            var minRequired = debts.Sum(d => d.MinimumPayment);
            decimal budget = Math.Max(monthlyBudget ?? user.PreferredMonthlyBudget, minRequired);

            var snowball = _calculator.Calculate(debts, budget, PayoffStrategy.Snowball);
            var avalanche = _calculator.Calculate(debts, budget, PayoffStrategy.Avalanche);

            var model = new SnowballComparisonViewModel
            {
                MonthlyBudget = budget,
                Snowball = snowball,
                Avalanche = avalanche
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePreferences(PayoffStrategy strategy, decimal budget, string action)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Index");

            if (action == "save")
            {
                user.PreferredStrategy = strategy;
                user.PreferredMonthlyBudget = budget;
                await _userManager.UpdateAsync(user);
            }

            // Redirect to the strategy (or Compare)
            if (strategy == PayoffStrategy.Compare)
            {
                return RedirectToAction("Compare", new { monthlyBudget = budget });
            }

            return RedirectToAction("Index", new { monthlyBudget = budget, strategy });
        }


    }
}
