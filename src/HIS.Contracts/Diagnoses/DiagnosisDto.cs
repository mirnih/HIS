namespace HIS.Contracts.Diagnoses;

public class DiagnosisDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}