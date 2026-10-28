using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.MedicalRecords;

public class AddDiagnosisRequest
{
    [Required]
    public int? MedicalWorkerId { get; set; }

    [Required]
    public int? DiagnosisId { get; set; }
}
