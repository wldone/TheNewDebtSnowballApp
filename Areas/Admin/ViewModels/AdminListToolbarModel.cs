namespace DebtSnowballApp.Areas.Admin.ViewModels
{
    public class AdminListToolbarModel
    {
        public string Title { get; set; } = "";
        public string CreateText { get; set; } = "Create";
        public string CreateUrl { get; set; } = "#";
        public string SearchPlaceholder { get; set; } = "Search…";
        public string QueryParamName { get; set; } = "q";
        public string CurrentQuery { get; set; } = "";
        // Persisted sort
        public string? Sort { get; set; }
        public string? Dir { get; set; }

        // NEW: Role filter (optional)
        public string RoleParamName { get; set; } = "role";
        public string? SelectedRole { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();

        // Optional extra GET params to persist (e.g., page)
        public IDictionary<string, string>? ExtraRouteValues { get; set; }
    }
}
