using System.ComponentModel.DataAnnotations;

public class QuickAnalysisPersonal
{
    public int Id { get; set; }

    public string? UserId { get; set; }       // Link to AspNetUsers
    public string? TempUserId { get; set; }   // Anonymous session ID

    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone Number (optional)")]
    public string? Phone { get; set; }
}
