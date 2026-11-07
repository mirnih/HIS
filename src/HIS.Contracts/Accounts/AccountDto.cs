using HIS.Contracts.Enums;

namespace HIS.Contracts.Accounts;

public class AccountDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdministrator { get; set; }
    public int? MedicalWorkerId { get; set; }
    public string? MedicalWorkerFullName { get; set; }
    public MedicalWorkerRole? MedicalWorkerRole { get; set; }
}
