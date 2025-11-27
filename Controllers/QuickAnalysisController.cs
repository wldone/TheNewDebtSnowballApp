using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using DebtSnowballApp.Services;
//using DebtSnowballApp.ViewModels.QuickAnalysisViewModel;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class QuickAnalysisController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IQuickAnalysisCalculator _quickCalc;


    public QuickAnalysisController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IQuickAnalysisCalculator quickCalc)
    {
        _context = context;
        _userManager = userManager;
        _quickCalc = quickCalc;
    }

    // helper
    private string GetCurrentId()
    {
        if (User.Identity?.IsAuthenticated == true)
            return _userManager.GetUserId(User)!;

        var tempUserId = HttpContext.Session.GetString("TempUserId");
        if (string.IsNullOrEmpty(tempUserId))
        {
            tempUserId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("TempUserId", tempUserId);
        }
        return tempUserId;
    }

    [HttpGet]
    public async Task<IActionResult> Step1Personal()
    {
        var id = GetCurrentId();

        var existing = await _context.QuickAnalysisPersonals
            .FirstOrDefaultAsync(p => p.UserId == id || p.TempUserId == id);

        var model = existing ?? new QuickAnalysisPersonal();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step1Personal(QuickAnalysisPersonal model)
    {
        var id = GetCurrentId();

        if (!ModelState.IsValid)
            return View(model);

        var existing = await _context.QuickAnalysisPersonals
            .FirstOrDefaultAsync(p => p.UserId == id || p.TempUserId == id);

        if (User.Identity?.IsAuthenticated == true)
            model.UserId = id;
        else
            model.TempUserId = id;

        if (existing == null)
        {
            _context.QuickAnalysisPersonals.Add(model);
        }
        else
        {
            existing.FirstName = model.FirstName;
            existing.LastName = model.LastName;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Step2Debts));   // next step in wizard
    }

    // STEP 2 - GET
    [HttpGet]
    public async Task<IActionResult> Step2Debts()
    {
        var id = GetCurrentId();

        var debts = await _context.QaDebtItems
            .Where(d => d.UserId == id || d.TempUserId == id)
            .OrderByDescending(d => d.Balance)
            .ToListAsync();

        var vm = new QuickAnalysisDebtsViewModel
        {
            ExistingDebts = debts,
            NewDebt = new QaDebtItem(),
            DebtTypeOptions = GetDebtTypeOptions()
        };

        // sensible default type
        vm.NewDebt.Name = "Credit Card";

        return View(vm);
    }

    // STEP 2 - POST (Add debt / Next / Previous)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step2Debts(
        QuickAnalysisDebtsViewModel vm,
        string submitAction)
    {
        var id = GetCurrentId();
        var isAuthenticated = User.Identity?.IsAuthenticated == true;   // <-- ADD THIS HERE

        switch (submitAction)
        {
            case "LoadDemo":

                // Remove existing debts for this session/user
                var existing = await _context.QaDebtItems
                    .Where(d => d.UserId == id || d.TempUserId == id)
                    .ToListAsync();

                if (existing.Any())
                    _context.QaDebtItems.RemoveRange(existing);

                // Add demo debts
                var demoDebts = GetDemoDebtsForOwner(id, isAuthenticated);
                await _context.QaDebtItems.AddRangeAsync(demoDebts);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Step2Debts));


            case "AddDebt":
                // whole-dollar guard (optional, from earlier)
                if (vm.NewDebt.Balance % 1 != 0 || vm.NewDebt.MinimumPayment % 1 != 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "Please enter whole dollar amounts (no cents) for balance and payment.");

                    vm.ExistingDebts = await _context.QaDebtItems
                        .Where(d => d.UserId == id || d.TempUserId == id)
                        .OrderByDescending(d => d.Balance)
                        .ToListAsync();
                    vm.DebtTypeOptions = GetDebtTypeOptions();
                    return View(vm);
                }

                if (!ModelState.IsValid)
                {
                    vm.ExistingDebts = await _context.QaDebtItems
                        .Where(d => d.UserId == id || d.TempUserId == id)
                        .OrderByDescending(d => d.Balance)
                        .ToListAsync();
                    vm.DebtTypeOptions = GetDebtTypeOptions();
                    return View(vm);
                }

                var debt = vm.NewDebt;

                if (User.Identity?.IsAuthenticated == true)
                    debt.UserId = id;
                else
                    debt.TempUserId = id;

                _context.QaDebtItems.Add(debt);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Step2Debts));

            case "Next":
                // ensure at least one debt exists before moving on
                var hasDebts = await _context.QaDebtItems
                    .AnyAsync(d => d.UserId == id || d.TempUserId == id);

                if (!hasDebts)
                {
                    ModelState.AddModelError(string.Empty,
                        "Please enter at least one debt before continuing.");

                    vm.ExistingDebts = new List<QaDebtItem>();
                    vm.DebtTypeOptions = GetDebtTypeOptions();
                    return View(vm);
                }

                return RedirectToAction(nameof(Step3Result));

            case "Previous":
                return RedirectToAction(nameof(Step1Personal));
        }

        // fallback: reload page
        vm.ExistingDebts = await _context.QaDebtItems
            .Where(d => d.UserId == id || d.TempUserId == id)
            .OrderByDescending(d => d.Balance)
            .ToListAsync();
        vm.DebtTypeOptions = GetDebtTypeOptions();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQaDebt(int id)
    {
        var ownerId = GetCurrentId();

        var debt = await _context.QaDebtItems
            .FirstOrDefaultAsync(d =>
                d.Id == id &&
                (d.UserId == ownerId || d.TempUserId == ownerId));

        if (debt != null)
        {
            // Save a simple snapshot for undo
            var dto = new LastDeletedDebtDto
            {
                Name = debt.Name,
                Balance = debt.Balance,
                InterestRate = debt.InterestRate,
                MinimumPayment = debt.MinimumPayment,
                UserId = debt.UserId,
                TempUserId = debt.TempUserId
            };

            TempData["LastDeletedDebt"] = JsonSerializer.Serialize(dto);

            _context.QaDebtItems.Remove(debt);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Step2Debts));
    }

    private class LastDeletedDebtDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal InterestRate { get; set; }
        public decimal MinimumPayment { get; set; }
        public string? UserId { get; set; }
        public string? TempUserId { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UndoDeleteQaDebt()
    {
        if (TempData.Peek("LastDeletedDebt") is not string json)
            return RedirectToAction(nameof(Step2Debts));

        var dto = JsonSerializer.Deserialize<LastDeletedDebtDto>(json);
        if (dto == null)
            return RedirectToAction(nameof(Step2Debts));

        var qaDebt = new QaDebtItem
        {
            Name = dto.Name,
            Balance = dto.Balance,
            InterestRate = dto.InterestRate,
            MinimumPayment = dto.MinimumPayment,
            UserId = dto.UserId,
            TempUserId = dto.TempUserId
        };

        _context.QaDebtItems.Add(qaDebt);
        await _context.SaveChangesAsync();

        // Clear so we don't undo twice
        TempData.Remove("LastDeletedDebt");

        return RedirectToAction(nameof(Step2Debts));
    }

    [HttpGet]
    public async Task<IActionResult> Step3Result()
    {
        var ownerId = GetCurrentId();                    // same helper you use in Step1/2
        var vm = await _quickCalc.CalculateAsync(ownerId);
        return View("Step3Result", vm);                 // make sure this matches your .cshtml name
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Step3Next()
    {
        // save anything needed, then move to step 4
        return RedirectToAction("Step4");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Step3Previous()
    {
        return RedirectToAction("Step2Debts");
    }
    private static readonly string[] QuickDebtTypes = new[]
    {
        "Mortgage",
        "Automobile",
        "Credit Card",
        "Student Loan",
        "Personal Loan",
        "Other"
    };

    private IEnumerable<SelectListItem> GetDebtTypeOptions()
    {
        return QuickDebtTypes.Select(t => new SelectListItem
        {
            Value = t,
            Text = t
        });
    }
    private List<QaDebtItem> GetDemoDebtsForOwner(string ownerId, bool isAuthenticated)
    {
        var demoDebts = new List<QaDebtItem>
    {
        new QaDebtItem
        {
            Name = "Mortgage",
            Balance = 200000,
            InterestRate = 7m,
            MinimumPayment = 1330
        },
        new QaDebtItem
        {
            Name = "Auto Loan",
            Balance = 25000,
            InterestRate = 5m,
            MinimumPayment = 450
        },
        new QaDebtItem
        {
            Name = "Auto Loan",
            Balance = 35000,
            InterestRate = 5m,
            MinimumPayment = 575
        },
        new QaDebtItem
        {
            Name = "Credit Card",
            Balance = 10000,
            InterestRate = 18m,
            MinimumPayment = 300
        },
        new QaDebtItem
        {
            Name = "Credit Card",
            Balance = 5000,
            InterestRate = 14m,
            MinimumPayment = 150
        },
        new QaDebtItem
        {
            Name = "Credit Card",
            Balance = 2500,
            InterestRate = 18m,
            MinimumPayment = 75
        },
        new QaDebtItem
        {
            Name = "Other",
            Balance = 2500,
            InterestRate = 21m,
            MinimumPayment = 250
        },
        new QaDebtItem
        {
            Name = "Other",
            Balance = 10000,
            InterestRate = 6m,
            MinimumPayment = 450
        }

        //new QaDebtItem
        //{
        //    Name = "Mortgage",
        //    Balance = 245000,
        //    InterestRate = 4.25m,
        //    MinimumPayment = 1450
        //},
        //new QaDebtItem
        //{
        //    Name = "Auto Loan",
        //    Balance = 18500,
        //    InterestRate = 6.9m,
        //    MinimumPayment = 385
        //},
        //new QaDebtItem
        //{
        //    Name = "Credit Card",
        //    Balance = 8200,
        //    InterestRate = 18.99m,
        //    MinimumPayment = 200
        //},
        //new QaDebtItem
        //{
        //    Name = "Student Loan",
        //    Balance = 26500,
        //    InterestRate = 5.5m,
        //    MinimumPayment = 275
        //}
    };

        foreach (var d in demoDebts)
        {
            if (isAuthenticated)
                d.UserId = ownerId;
            else
                d.TempUserId = ownerId;
        }

        return demoDebts;
    }
    private async Task<QuickAnalysisResultViewModel> BuildSummaryAsync()
    {
        var id = GetCurrentId();

        // Load personal info
        var person = await _context.QuickAnalysisPersonals
            .FirstOrDefaultAsync(p => p.UserId == id || p.TempUserId == id);

        var debts = await _context.QaDebtItems
            .Where(d => d.UserId == id || d.TempUserId == id)
            .ToListAsync();

        if (person == null || debts.Count == 0)
            throw new Exception("Missing personal data or debts.");

        // ---------------------------
        // PERFORM YOUR CALCULATIONS
        // ---------------------------

        // Total debt
        var totalDebt = debts.Sum(d => d.BegBalance);

        // Monthly debt
        var monthly = debts.Sum(d => d.MinimumPayment);

        // Debt freedom
        var yearsCurrent = 30.1;     // placeholder
        var yearsPlan = 9.3;         // placeholder

        // Principal & interest
        decimal pAndICurrent = 595972m;
        decimal pAndIPlan = 393944m;

        // Savings
        decimal savings = pAndICurrent - pAndIPlan;

        // Cost per day
        decimal costPerDay = Math.Round(savings / 365m, 2);

        // Wealth builder
        decimal wealth = 1570454m;

        return new QuickAnalysisResultViewModel
        {
            ClientName = $"{person.FirstName} {person.LastName.FirstOrDefault()}.",

            CurrentTotalDebt = totalDebt,
            CurrentMonthlyDebt = monthly,
            CurrentDebtFreedomYears = yearsCurrent,
            CurrentPrincipalAndInterest = pAndICurrent,

            PlanTotalDebt = totalDebt,
            PlanMonthlyDebt = monthly,
            PlanDebtFreedomYears = yearsPlan,
            PlanPrincipalAndInterest = pAndIPlan,

            CostPerDay = costPerDay,

            WealthBuilderTotalWealth = wealth,
            WealthBuilderMonthlyContribution = monthly,
            WealthBuilderPeriodYears = 20.8,
            WealthBuilderRoiPercent = 5
        };
    }

}
