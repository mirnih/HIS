namespace HIS.Contracts.MedicalRecords;

public class MedicalRecordHistoryDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int MedicalWorkerId { get; set; }

    public string MedicalWorkerFullName { get; set; } = string.Empty;

    public MedicalRecordAction Action { get; set; }

    public string Data { get; set; } = string.Empty;
}
