using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.Accounts;

public class CreateMedicalWorkerAccountRequest
{
    [Required]
    [StringLength(16, MinimumLength = 3)]
    [RegularExpression(
        @"^[A-Za-z0-9._-]+$",
        ErrorMessage = "Логин может содержать латинские буквы, цифры, точку, дефис и подчёркивание.")]
    public string Username { get; set; } = string.Empty;
}
