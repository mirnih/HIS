namespace HIS.Contracts.MedicalRecords;

public class PrescriptionDto
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
