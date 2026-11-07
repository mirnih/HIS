using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.Enums;
using HIS.Contracts.MedicalWorkers;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class MedicalWorkerService
{
    private readonly HisDbContext _db;
    private readonly AuditService _auditService;
    private readonly RefreshTokenService _refreshTokenService;

    public MedicalWorkerService(
        HisDbContext db,
        AuditService auditService,
        RefreshTokenService refreshTokenService)
    {
        _db = db;
        _auditService = auditService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<List<MedicalWorkerDto>> GetAllAsync()
    {
        return await _db.MedicalWorkers
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new MedicalWorkerDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Role = x.Role,
                Position = x.Position,
                IsDeleted = x.IsDeleted
            })
            .ToListAsync();
    }

    public async Task<MedicalWorkerDto?> GetByIdAsync(int id)
    {
        return await _db.MedicalWorkers
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new MedicalWorkerDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Position = x.Position,
                Role = x.Role,
                IsDeleted = x.IsDeleted
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MedicalWorkerDto> CreateAsync(
        CreateMedicalWorkerRequest request,
        Guid actorAccountId,
        string actor)
    {
        var worker = new MedicalWorker
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Position = request.Position.Trim(),
            Role = MedicalWorkerRole.Other
        };

        _db.MedicalWorkers.Add(worker);

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Добавлен медработник {worker.LastName} {worker.FirstName}.");

        await _db.SaveChangesAsync();

        return new MedicalWorkerDto
        {
            Id = worker.Id,
            FirstName = worker.FirstName,
            LastName = worker.LastName,
            Position = worker.Position,
            Role = worker.Role,
            IsDeleted = worker.IsDeleted
        };
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateMedicalWorkerRequest request,
        Guid actorAccountId,
        string actor)
    {
        var worker = await _db.MedicalWorkers.FindAsync(id);

        if (worker is null || worker.IsDeleted)
        {
            return false;
        }

        worker.FirstName = request.FirstName.Trim();
        worker.LastName = request.LastName.Trim();
        worker.Position = request.Position.Trim();

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Изменены данные медработника {worker.LastName} {worker.FirstName}.");

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        Guid actorAccountId,
        string actor)
    {
        var worker = await _db.MedicalWorkers
            .Include(x => x.Account)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (worker is null || worker.IsDeleted)
        {
            return false;
        }

        worker.IsDeleted = true;

        var revokedCount = 0;

        if (worker.Account is not null)
        {
            worker.Account.IsActive = false;
            worker.Account.UpdatedAt = DateTime.UtcNow;
            revokedCount = await _refreshTokenService
                .RevokeAllAsync(worker.Account.Id);
        }

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Медработник {worker.LastName} {worker.FirstName} " +
            $"помечен удалённым; отозвано refresh tokens: {revokedCount}.");

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateRoleAsync(
        int id,
        UpdateMedicalWorkerRoleRequest request,
        Guid actorAccountId,
        string actor)
    {
        var worker = await _db.MedicalWorkers
            .Include(x => x.Account)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (worker is null || worker.IsDeleted)
        {
            return false;
        }

        var oldRole = worker.Role;

        if (oldRole == request.Role)
        {
            return true;
        }

        worker.Role = request.Role;

        var revokedCount = worker.Account is null
            ? 0
            : await _refreshTokenService.RevokeAllAsync(
                worker.Account.Id);

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Роль медработника {worker.LastName} {worker.FirstName} " +
            $"изменена: {oldRole} → {worker.Role}; " +
            $"отозвано refresh tokens: {revokedCount}.");

        await _db.SaveChangesAsync();

        return true;
    }
}
