namespace HIS.Api.Models;

public class MedicalRecord
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    
    public List<MedicalRecordDiagnosis> Diagnoses { get; set; } = [];

    public List<Prescription> Prescriptions { get; set; } = [];

    public List<MedicalRecordHistory> History { get; set; } = [];
}