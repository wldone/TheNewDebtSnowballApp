using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DebtSnowballApp.Models
{
    public class QuickAnalysisDebtsViewModel
    {
        // New debt row user is entering
        public QaDebtItem NewDebt { get; set; } = new QaDebtItem();

        // Existing debts for this user/temp user
        public List<QaDebtItem> ExistingDebts { get; set; } = new List<QaDebtItem>();

        // Dropdown options for Type of Debt (maps to QaDebtItem.Name)
        public IEnumerable<SelectListItem> DebtTypeOptions { get; set; }
            = new List<SelectListItem>();
    }
}




