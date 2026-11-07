using System.ComponentModel.DataAnnotations;
using HIS.Contracts.Enums;

namespace HIS.Api.Models;

public class MedicalWorker
{
    public int Id { get; set; }

    public MedicalWorkerAccount? Account { get; set; }

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    public MedicalWorkerRole Role { get; set; } = MedicalWorkerRole.Other;

    [Required]
    [StringLength(24, MinimumLength = 3)]
    public string Position { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

}