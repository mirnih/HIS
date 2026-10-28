using System.ComponentModel.DataAnnotations;

namespace HIS.Api.Models;

public class MedicalWorker
{
    public int Id { get; set; }

    [Required]
    [StringLength(24, MinimumLength = 3)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(24, MinimumLength = 3)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(24, MinimumLength = 3)]
    public string Position { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

}