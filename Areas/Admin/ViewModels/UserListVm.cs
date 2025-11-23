namespace DebtSnowballApp.Areas.Admin.ViewModels
{
    public record UserListVm(string Id, 
        string Email,
        string? Partner, 
        bool Locked, 
        IEnumerable<string> Roles);

}
