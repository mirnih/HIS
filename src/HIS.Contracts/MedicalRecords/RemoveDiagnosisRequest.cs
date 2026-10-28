using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.MedicalRecords;

public class RemoveDiagnosisRequest
{
    [Required]
    public int? MedicalWorkerId { get; set; }
}
