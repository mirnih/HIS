namespace HIS.Contracts.MedicalRecords;

public class MedicalRecordDto
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<MedicalRecordDiagnosisDto> Diagnoses { get; set; } = [];

    public List<PrescriptionDto> Prescriptions { get; set; } = [];
}
