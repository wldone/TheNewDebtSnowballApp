using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtSnowballApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // restrict the area to Admins
    public class DashboardController : Controller
    {
        public IActionResult Index() => View();
    }
}
