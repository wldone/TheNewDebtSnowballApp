using DebtSnowballApp.Models;

namespace DebtSnowballApp.Areas.Admin.ViewModels
{
    public class UserProfileVm
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserName { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        public string? PartnerName { get; set; }
        public bool Locked { get; set; }

        public PayoffStrategy PreferredStrategy { get; set; }
        public decimal PreferredMonthlyBudget { get; set; }

        public string FullName =>
            string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public string AddressOneLine =>
            string.Join(", ", new[]
            {
                Address1,
                Address2,
                string.Join(" ", new[]{ City, State }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                PostalCode,
                Country
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
