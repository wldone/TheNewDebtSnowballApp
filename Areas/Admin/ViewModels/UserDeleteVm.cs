using System.ComponentModel.DataAnnotations;

namespace DebtSnowballApp.Areas.Admin.ViewModels
{
    public class UserDeleteVm
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Optional safety flags for the UI
        public bool IsSelf { get; set; }             // current logged-in user == target
        public bool IsOnlyAdmin { get; set; }        // deleting would remove last Admin
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
