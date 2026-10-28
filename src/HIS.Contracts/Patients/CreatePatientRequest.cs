using System.ComponentModel.DataAnnotations;
using HIS.Contracts.Enums;

namespace HIS.Contracts.Patients;

public class CreatePatientRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(50, MinimumLength = 2)]
    public string? MiddleName { get; set; }

    [Required]
    public DateTime? BirthDate { get; set; }

    [Required]
    public AdministrativeGender? Gender { get; set; }

    [StringLength(32)]
    public string? Comment { get; set; }
}