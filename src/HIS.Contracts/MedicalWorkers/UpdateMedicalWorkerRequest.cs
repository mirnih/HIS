using System.ComponentModel.DataAnnotations;

namespace HIS.Contracts.MedicalWorkers;

public class UpdateMedicalWorkerRequest
{
    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(24, MinimumLength = 3)]
    public string Position { get; set; } = string.Empty;

}