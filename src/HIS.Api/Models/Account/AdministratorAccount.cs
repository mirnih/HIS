using System.ComponentModel.DataAnnotations;

namespace HIS.Api.Models;

public class AdministratorAccount : Account
{
    [StringLength(128)]
    public string? TotpSecret { get; set; }

    public bool IsTotpConfigured { get; set; }
}