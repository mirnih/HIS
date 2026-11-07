using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.Auth;

public class ConfirmAdminSetupRequest
{
    [Required]
    [RegularExpression(
        @"^[0-9]{6}$",
        ErrorMessage = "Код должен содержать 6 цифр.")]
    public string Code { get; set; } = string.Empty;
}
