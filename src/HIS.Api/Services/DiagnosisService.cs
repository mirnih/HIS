using System.Text.Json;
using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.Diagnoses;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class DiagnosisService
{
    private readonly HisDbContext _db;

    public DiagnosisService(HisDbContext db)
    {
        _db = db;
    }

    public async Task<List<DiagnosisDto>> GetAllAsync()
    {
        return await _db.Diagnoses
            .OrderBy(x => x.Code)
            .Select(x => new DiagnosisDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name
            })
            .ToListAsync();
    }

    public async Task<DiagnosisDto?> GetByIdAsync(int id)
    {
        return await _db.Diagnoses
            .Where(x => x.Id == id)
            .Select(x => new DiagnosisDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(DiagnosisOperationResult Result, DiagnosisDto? Diagnosis)> CreateAsync(CreateDiagnosisRequest request)
    {
        var exists = await _db.Diagnoses.AnyAsync(x =>
            x.Code == request.Code ||
            x.Name == request.Name);

        if (exists)
        {
            return (DiagnosisOperationResult.Conflict, null);
        }

        var diagnosis = new Diagnosis
        {
            Code = request.Code,
            Name = request.Name
        };

        _db.Diagnoses.Add(diagnosis);
        await _db.SaveChangesAsync();

        return (
            DiagnosisOperationResult.Success,
            new DiagnosisDto
            {
                Id = diagnosis.Id,
                Code = diagnosis.Code,
                Name = diagnosis.Name
            });
    }

    public async Task<DiagnosisOperationResult> UpdateAsync(int id, UpdateDiagnosisRequest request)
    {
        var diagnosis = await _db.Diagnoses.FindAsync(id);

        if (diagnosis is null)
        {
            return DiagnosisOperationResult.NotFound;
        }

        var duplicateExists = await _db.Diagnoses.AnyAsync(x =>
            x.Id != id &&
            (x.Code == request.Code || x.Name == request.Name));

        if (duplicateExists)
        {
            return DiagnosisOperationResult.Conflict;
        }

        diagnosis.Code = request.Code;
        diagnosis.Name = request.Name;

        await _db.SaveChangesAsync();

        return DiagnosisOperationResult.Success;
    }

    public async Task<DiagnosisOperationResult> DeleteAsync(int id)
    {
        var diagnosis = await _db.Diagnoses.FindAsync(id);

        if (diagnosis is null)
        {
            return DiagnosisOperationResult.NotFound;
        }

        var isUsed = await _db.MedicalRecordDiagnoses
            .AnyAsync(x => x.DiagnosisId == id);

        if (isUsed)
        {
            return DiagnosisOperationResult.InUse;
        }

        _db.Diagnoses.Remove(diagnosis);
        await _db.SaveChangesAsync();

        return DiagnosisOperationResult.Success;
    }

    public async Task<int> ImportAsync(Stream stream)
    {
        var items = await JsonSerializer.DeserializeAsync<
            List<CreateDiagnosisRequest>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (items is null)
        {
            return 0;
        }

        var imported = 0;

        foreach (var item in items)
        {
            var exists = await _db.Diagnoses.AnyAsync(x =>
                x.Code == item.Code ||
                x.Name == item.Name);

            if (exists)
            {
                continue;
            }

            _db.Diagnoses.Add(new Diagnosis
            {
                Code = item.Code,
                Name = item.Name
            });

            imported++;
        }

        await _db.SaveChangesAsync();

        return imported;
    }
}