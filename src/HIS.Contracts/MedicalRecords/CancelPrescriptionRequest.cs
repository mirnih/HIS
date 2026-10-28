using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.MedicalRecords;

public class CancelPrescriptionRequest
{
    [Required]
    public int? MedicalWorkerId { get; set; }
}
