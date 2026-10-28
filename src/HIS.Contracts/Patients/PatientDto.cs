using HIS.Contracts.Enums;

namespace HIS.Contracts.Patients;

public class PatientDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public DateTime BirthDate { get; set; }

    public AdministrativeGender Gender { get; set; }

    public string? Comment { get; set; }

    public bool HasMedicalRecord { get; set; }
}