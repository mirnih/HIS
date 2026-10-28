using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.MedicalRecords;

public class AddPrescriptionRequest
{
    [Required]
    public int? MedicalWorkerId { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 3)]
    public string Text { get; set; } = string.Empty;
}
