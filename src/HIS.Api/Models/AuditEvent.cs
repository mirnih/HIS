using System.ComponentModel.DataAnnotations;
namespace HIS.Api.Models;

public class AuditEvent
{
    public Guid EventId { get; set; }
    public DateTime EventTime { get; set; }

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

    [Required]
    [StringLength(128)]
    public string Actor { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

}