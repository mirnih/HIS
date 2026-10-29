using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.Audit;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class AuditService
{
    private readonly HisDbContext _db;

    public AuditService(HisDbContext db)
    {
        _db = db;
    }

    public void AddEvent(
        Guid? accountId,
        string actor,
        string message)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventTime = DateTime.UtcNow,
            AccountId = accountId,
            Actor = actor,
            Message = message
        });
    }

    public Task<List<AuditEventDto>> GetAllAsync()
    {
        return _db.AuditEvents
            .AsNoTracking()
            .OrderByDescending(x => x.EventTime)
            .ThenByDescending(x => x.EventId)
            .Select(x => new AuditEventDto
            {
                EventId = x.EventId,
                EventTime = x.EventTime,
                AccountId = x.AccountId,
                Actor = x.Actor,
                Message = x.Message
            })
            .ToListAsync();
    }
}
