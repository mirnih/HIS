using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.MedicalRecords;

public class AddDiagnosisRequest
{
    [Range(1, int.MaxValue)]
    public int DiagnosisId { get; set; }
}
