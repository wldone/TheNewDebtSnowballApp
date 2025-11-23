using DebtSnowballApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DebtSnowballApp.ViewModels
{
    public class DebtItemViewModel
    {
        public DebtItem DebtItem { get; set; } = new();

        public IEnumerable<SelectListItem>? DebtTypeList { get; set; }
    }
}
