namespace HIS.Contracts.Audit;

public class AuditEventDto
{
    public Guid EventId { get; set; }
    public DateTime EventTime { get; set; }
    public Guid? AccountId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
