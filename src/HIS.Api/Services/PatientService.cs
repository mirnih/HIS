using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.Patients;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class PatientService
{
    private readonly HisDbContext _db;
    private readonly AuditService _auditService;

    public PatientService(
        HisDbContext db,
        AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task<List<PatientDto>> GetAllAsync()
    {
        return await _db.Patients
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new PatientDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                MiddleName = x.MiddleName,
                BirthDate = x.BirthDate,
                Gender = x.Gender,
                Comment = x.Comment,
                HasMedicalRecord = x.MedicalRecord != null
            })
            .ToListAsync();
    }

    public async Task<PatientDto?> GetByIdAsync(int id)
    {
        return await _db.Patients
            .Where(x => x.Id == id)
            .Select(x => new PatientDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                MiddleName = x.MiddleName,
                BirthDate = x.BirthDate,
                Gender = x.Gender,
                Comment = x.Comment,
                HasMedicalRecord = x.MedicalRecord != null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PatientDto> CreateAsync(
        CreatePatientRequest request,
        Guid actorAccountId,
        string actor)
    {
        var patient = new Patient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            BirthDate = request.BirthDate!.Value,
            Gender = request.Gender!.Value,
            Comment = request.Comment
        };

        _db.Patients.Add(patient);

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Добавлен пациент {patient.LastName} {patient.FirstName}.");

        await _db.SaveChangesAsync();

        return new PatientDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            MiddleName = patient.MiddleName,
            BirthDate = patient.BirthDate,
            Gender = patient.Gender,
            Comment = patient.Comment,
            HasMedicalRecord = false
        };
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdatePatientRequest request,
        Guid actorAccountId,
        string actor)
    {
        var patient = await _db.Patients.FindAsync(id);

        if (patient is null)
        {
            return false;
        }

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.MiddleName = request.MiddleName;
        patient.BirthDate = request.BirthDate!.Value;
        patient.Gender = request.Gender!.Value;
        patient.Comment = request.Comment;

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Изменены данные пациента {patient.LastName} {patient.FirstName}.");

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<PatientDeleteResult> DeleteAsync(
        int id,
        Guid actorAccountId,
        string actor)
    {
        var patient = await _db.Patients.FindAsync(id);

        if (patient is null)
        {
            return PatientDeleteResult.NotFound;
        }

        var hasMedicalRecord = await _db.MedicalRecords
            .AnyAsync(x => x.PatientId == id);

        if (hasMedicalRecord)
        {
            return PatientDeleteResult.HasMedicalRecord;
        }

        _db.Patients.Remove(patient);

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Удалён пациент {patient.LastName} {patient.FirstName}.");

        await _db.SaveChangesAsync();

        return PatientDeleteResult.Success;
    }
}
