namespace HIS.Api.Models;

public class MedicalRecordHistory
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public MedicalRecord MedicalRecord { get; set; } = null!;

    public int MedicalWorkerId { get; set; }

    public MedicalWorker MedicalWorker { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public MedicalRecordAction Action { get; set; }

    public string Data { get; set; } = string.Empty;
}