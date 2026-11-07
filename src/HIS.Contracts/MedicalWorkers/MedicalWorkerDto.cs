using HIS.Contracts.Enums;

namespace HIS.Contracts.MedicalWorkers;

public class MedicalWorkerDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public MedicalWorkerRole Role { get; set; }

    public string Position { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

}