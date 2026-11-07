using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.Auth;

public class StartAdminSetupRequest
{
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}