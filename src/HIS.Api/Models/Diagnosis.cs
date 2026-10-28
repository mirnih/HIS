using System.ComponentModel.DataAnnotations;

namespace HIS.Api.Models;

public class Diagnosis
{
    public int Id { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(24, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
}