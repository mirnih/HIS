namespace HIS.Api.Models;

public class MedicalRecordDiagnosis
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public MedicalRecord MedicalRecord { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public Diagnosis Diagnosis { get; set; } = null!; 
}