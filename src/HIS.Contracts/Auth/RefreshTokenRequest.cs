using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
