using DebtSnowballApp.Data;
using DebtSnowballApp.Models;
using DebtSnowballApp.Services;
//using DebtSnowballApp.ViewModels.QuickAnalysisViewModel;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

public class QuickAnalysisController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IQuickAnalysisCalculator _quickCalc;
    //private readonly EmailBuilder _emailBuilder;
    private readonly IEmailService _emailService;

    public QuickAnalysisController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IQuickAnalysisCalculator quickCalc,
        //EmailBuilder emailBuilder,
        IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _quickCalc = quickCalc;
        //_emailBuilder = emailBuilder;
        _emailService = emailService;
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
    private const string QaEmailSessionKey = "QA_Email";

    private void SaveQaEmail(string? email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            HttpContext.Session.SetString(QaEmailSessionKey, email);
        }
    }

    private string? GetQaEmail()
    {
        return HttpContext.Session.GetString(QaEmailSessionKey);
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
        HttpContext.Session.SetString("QA_FirstName", model.FirstName);
        HttpContext.Session.SetString("QA_LastName", model.LastName);

        await _context.SaveChangesAsync();

        SaveQaEmail(model.Email);

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
                TempData.Remove("LastDeletedDebt");
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
                TempData.Remove("LastDeletedDebt");
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
                TempData.Remove("LastDeletedDebt");
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
        HttpContext.Session.SetString("QA_FullName", vm.ClientName);
        TempData.Remove("LastDeletedDebt");
        return View("Step3Result", vm);                 // make sure this matches your .cshtml name
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step3Next()
    {
         var ownerId = GetCurrentId();                    // same helper you use in Step1/2
       var model = await _quickCalc.CalculateAsync(ownerId); if (model == null)
            return RedirectToAction(nameof(Step2Debts));   // or some error page

        var email = model.ClientEmail;   // ⬅️ from Step 1
        if (!string.IsNullOrWhiteSpace(email))
        {
            string subject = "Your Quick Analysis Results – My Financial Outlook";
            string bodyHtml = EmailBuilder.BuildStep3EmailBody(model);

            //await _emailService.SendAsync(email, subject, bodyHtml);
        }

        return RedirectToAction(nameof(Step4Order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Step3Previous()
    {
        return RedirectToAction("Step2Debts");
    }

    [HttpGet]
    public IActionResult Step4Order(string? email)
    {
        // 1. If email passed via query string, store in session
        if (!string.IsNullOrWhiteSpace(email))
        {
            HttpContext.Session.SetString("QA_Email", email);
        }

        // 2. Pull from session (this ensures a single source of truth)
        var sessionEmail = HttpContext.Session.GetString("QA_Email");
        var fullName = HttpContext.Session.GetString("QA_FullName");

        // 3. Create the viewmodel
        var vm = new QuickAnalysisOrderViewModel
        {
            NameOnCard = fullName ?? string.Empty,
            ExpirationMonth = DateTime.UtcNow.Month,
            ExpirationYear = DateTime.UtcNow.Year,
            Email = sessionEmail // optional, only if your view model has Email
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Step4Order(QuickAnalysisOrderViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // TODO: hook into real subscription / stripe / etc.
        // For now, just send them to a simple thank-you page.
        return RedirectToAction("Step4ThankYou");
    }

    [HttpGet]
    public IActionResult Step4ThankYou()
    {
        var email = HttpContext.Session.GetString("QA_Email");
        var clientName = HttpContext.Session.GetString("QA_FullName");

        var vm = new QuickAnalysisOrderViewModel
        {
            NameOnCard = clientName ?? string.Empty,
            Email = email ?? string.Empty
        };

        return View(vm);
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
