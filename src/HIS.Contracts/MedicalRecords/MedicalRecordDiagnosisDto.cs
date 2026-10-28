namespace HIS.Contracts.MedicalRecords;

public class MedicalRecordDiagnosisDto
{
    public int Id { get; set; }

    public int DiagnosisId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
