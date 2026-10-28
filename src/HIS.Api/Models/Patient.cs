using System.ComponentModel.DataAnnotations;
using HIS.Contracts.Enums;

namespace HIS.Api.Models;

public class Patient
{
    public int Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(50, MinimumLength = 2)]
    public string? MiddleName { get; set; }

    public DateTime BirthDate { get; set; }

    public AdministrativeGender Gender { get; set; }

    [StringLength(32)]
    public string? Comment { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }
}