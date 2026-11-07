using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.Auth;

public class LoginRequest
{
    [Required]
    [StringLength(16, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [RegularExpression(
        @"^[0-9]{6}$",
        ErrorMessage = "Код должен содержать 6 цифр.")]
    public string? TotpCode { get; set; }
}
