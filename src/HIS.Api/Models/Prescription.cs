using System.ComponentModel.DataAnnotations;

namespace HIS.Api.Models;

public class Prescription
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }

    public MedicalRecord MedicalRecord { get; set; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 3)]
    public string Text { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}