using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.MedicalWorkers;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class MedicalWorkerService
{
    private readonly HisDbContext _db;

    public MedicalWorkerService(HisDbContext db)
    {
        _db = db;
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
                IsDeleted = x.IsDeleted
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MedicalWorkerDto> CreateAsync(
        CreateMedicalWorkerRequest request)
    {
        var worker = new MedicalWorker
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Position = request.Position
        };

        _db.MedicalWorkers.Add(worker);
        await _db.SaveChangesAsync();

        return new MedicalWorkerDto
        {
            Id = worker.Id,
            FirstName = worker.FirstName,
            LastName = worker.LastName,
            Position = worker.Position,
            IsDeleted = worker.IsDeleted
        };
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateMedicalWorkerRequest request)
    {
        var worker = await _db.MedicalWorkers.FindAsync(id);

        if (worker is null || worker.IsDeleted)
        {
            return false;
        }

        worker.FirstName = request.FirstName;
        worker.LastName = request.LastName;
        worker.Position = request.Position;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var worker = await _db.MedicalWorkers.FindAsync(id);

        if (worker is null || worker.IsDeleted)
        {
            return false;
        }

        worker.IsDeleted = true;

        await _db.SaveChangesAsync();

        return true;
    }
}