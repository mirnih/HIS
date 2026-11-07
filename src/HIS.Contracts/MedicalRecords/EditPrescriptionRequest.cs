using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.MedicalRecords;

public class EditPrescriptionRequest
{
    [Required]
    [StringLength(128, MinimumLength = 3)]
    public string Text { get; set; } = string.Empty;
}
