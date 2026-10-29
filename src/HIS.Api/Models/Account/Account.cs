using System.ComponentModel.DataAnnotations;

namespace HIS.Api.Models;

public abstract class Account
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(16, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}