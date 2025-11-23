using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtSnowballApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // optional but recommended
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View(); // your dashboard view
    }
}
