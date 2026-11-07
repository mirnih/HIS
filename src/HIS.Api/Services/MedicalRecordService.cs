using System.Text.Json;
using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.MedicalRecords;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class MedicalRecordService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HisDbContext _db;
    private readonly AuditService _auditService;

    public MedicalRecordService(
        HisDbContext db,
        AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task<MedicalRecordDto?> GetByPatientIdAsync(int patientId)
    {
        var medicalRecord = await _db.MedicalRecords
            .AsNoTracking()
            .Include(x => x.Diagnoses)
                .ThenInclude(x => x.Diagnosis)
            .Include(x => x.Prescriptions)
            .FirstOrDefaultAsync(x => x.PatientId == patientId);

        return medicalRecord is null ? null : ToDto(medicalRecord);
    }

    public async Task<(MedicalRecordOperationResult Result, MedicalRecordDto? MedicalRecord)> CreateAsync(
        int patientId,
        Guid actorAccountId,
        string actor)
    {
        if (!await _db.Patients.AnyAsync(x => x.Id == patientId))
        {
            return (MedicalRecordOperationResult.NotFound, null);
        }

        if (await _db.MedicalRecords.AnyAsync(x => x.PatientId == patientId))
        {
            return (MedicalRecordOperationResult.Conflict, null);
        }

        var medicalRecord = new MedicalRecord
        {
            PatientId = patientId,
            CreatedAt = DateTime.UtcNow
        };

        _db.MedicalRecords.Add(medicalRecord);

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Создана ЭМК пациента с идентификатором {patientId}.");

        await _db.SaveChangesAsync();

        return (MedicalRecordOperationResult.Success, ToDto(medicalRecord));
    }

    public async Task<MedicalRecordOperationResult> AddDiagnosisAsync(
        int medicalRecordId,
        AddDiagnosisRequest request,
        int medicalWorkerId,
        Guid actorAccountId,
        string actor)
    {
        if (!await MedicalRecordExistsAsync(medicalRecordId))
        {
            return MedicalRecordOperationResult.NotFound;
        }

        var diagnosis = await _db.Diagnoses.FindAsync(request.DiagnosisId);

        if (diagnosis is null)
        {
            return MedicalRecordOperationResult.NotFound;
        }

        if (await _db.MedicalRecordDiagnoses.AnyAsync(x =>
            x.MedicalRecordId == medicalRecordId &&
            x.DiagnosisId == diagnosis.Id))
        {
            return MedicalRecordOperationResult.Conflict;
        }

        _db.MedicalRecordDiagnoses.Add(new MedicalRecordDiagnosis
        {
            MedicalRecordId = medicalRecordId,
            DiagnosisId = diagnosis.Id
        });

        _db.MedicalRecordHistory.Add(CreateHistory(
            medicalRecordId,
            medicalWorkerId,
            MedicalRecordAction.DiagnosisAdded,
            new
            {
                DiagnosisId = diagnosis.Id,
                diagnosis.Code,
                diagnosis.Name
            }));

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"В ЭМК {medicalRecordId} добавлен диагноз {diagnosis.Code}.");

        await _db.SaveChangesAsync();

        return MedicalRecordOperationResult.Success;
    }

    public async Task<MedicalRecordOperationResult> RemoveDiagnosisAsync(
        int medicalRecordId,
        int diagnosisId,
        int medicalWorkerId,
        Guid actorAccountId,
        string actor)
    {
        if (!await MedicalRecordExistsAsync(medicalRecordId))
        {
            return MedicalRecordOperationResult.NotFound;
        }

        var medicalRecordDiagnosis = await _db.MedicalRecordDiagnoses
            .Include(x => x.Diagnosis)
            .FirstOrDefaultAsync(x =>
                x.MedicalRecordId == medicalRecordId &&
                x.DiagnosisId == diagnosisId);

        if (medicalRecordDiagnosis is null)
        {
            return MedicalRecordOperationResult.NotFound;
        }

        _db.MedicalRecordDiagnoses.Remove(medicalRecordDiagnosis);

        _db.MedicalRecordHistory.Add(CreateHistory(
            medicalRecordId,
            medicalWorkerId,
            MedicalRecordAction.DiagnosisRemoved,
            new
            {
                DiagnosisId = medicalRecordDiagnosis.DiagnosisId,
                medicalRecordDiagnosis.Diagnosis.Code,
                medicalRecordDiagnosis.Diagnosis.Name
            }));

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Из ЭМК {medicalRecordId} удалён диагноз " +
            $"{medicalRecordDiagnosis.Diagnosis.Code}.");

        await _db.SaveChangesAsync();

        return MedicalRecordOperationResult.Success;
    }

    public async Task<(MedicalRecordOperationResult Result, PrescriptionDto? Prescription)> AddPrescriptionAsync(
        int medicalRecordId,
        AddPrescriptionRequest request,
        int medicalWorkerId,
        Guid actorAccountId,
        string actor)
    {
        if (!await MedicalRecordExistsAsync(medicalRecordId))
        {
            return (MedicalRecordOperationResult.NotFound, null);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var prescription = new Prescription
        {
            MedicalRecordId = medicalRecordId,
            Text = request.Text,
            IsActive = true
        };

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        _db.MedicalRecordHistory.Add(CreateHistory(
            medicalRecordId,
            medicalWorkerId,
            MedicalRecordAction.PrescriptionAdded,
            new
            {
                PrescriptionId = prescription.Id,
                prescription.Text
            }));

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"В ЭМК {medicalRecordId} добавлено назначение {prescription.Id}.");

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (
            MedicalRecordOperationResult.Success,
            new PrescriptionDto
            {
                Id = prescription.Id,
                Text = prescription.Text,
                IsActive = prescription.IsActive
            });
    }

    public async Task<MedicalRecordOperationResult> CancelPrescriptionAsync(
        int medicalRecordId,
        int prescriptionId,
        int medicalWorkerId,
        Guid actorAccountId,
        string actor)
    {
        if (!await MedicalRecordExistsAsync(medicalRecordId))
        {
            return MedicalRecordOperationResult.NotFound;
        }

        var prescription = await _db.Prescriptions.FirstOrDefaultAsync(x =>
            x.Id == prescriptionId &&
            x.MedicalRecordId == medicalRecordId);

        if (prescription is null)
        {
            return MedicalRecordOperationResult.NotFound;
        }

        if (!prescription.IsActive)
        {
            return MedicalRecordOperationResult.Conflict;
        }

        prescription.IsActive = false;

        _db.MedicalRecordHistory.Add(CreateHistory(
            medicalRecordId,
            medicalWorkerId,
            MedicalRecordAction.PrescriptionCancelled,
            new
            {
                PrescriptionId = prescription.Id,
                prescription.Text
            }));

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"В ЭМК {medicalRecordId} отменено назначение {prescription.Id}.");

        await _db.SaveChangesAsync();

        return MedicalRecordOperationResult.Success;
    }

    public async Task<(MedicalRecordOperationResult Result, PrescriptionDto? Prescription)> EditPrescriptionAsync(
        int medicalRecordId,
        int prescriptionId,
        EditPrescriptionRequest request,
        int medicalWorkerId,
        Guid actorAccountId,
        string actor)
    {
        if (!await MedicalRecordExistsAsync(medicalRecordId))
        {
            return (MedicalRecordOperationResult.NotFound, null);
        }

        var oldPrescription = await _db.Prescriptions.FirstOrDefaultAsync(x =>
            x.Id == prescriptionId &&
            x.MedicalRecordId == medicalRecordId);

        if (oldPrescription is null)
        {
            return (MedicalRecordOperationResult.NotFound, null);
        }

        if (!oldPrescription.IsActive)
        {
            return (MedicalRecordOperationResult.Conflict, null);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        oldPrescription.IsActive = false;

        var newPrescription = new Prescription
        {
            MedicalRecordId = medicalRecordId,
            Text = request.Text,
            IsActive = true
        };

        _db.Prescriptions.Add(newPrescription);
        await _db.SaveChangesAsync();

        _db.MedicalRecordHistory.Add(CreateHistory(
            medicalRecordId,
            medicalWorkerId,
            MedicalRecordAction.PrescriptionEdited,
            new
            {
                OldPrescriptionId = oldPrescription.Id,
                OldText = oldPrescription.Text,
                NewPrescriptionId = newPrescription.Id,
                NewText = newPrescription.Text
            }));

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"В ЭМК {medicalRecordId} изменено назначение " +
            $"{oldPrescription.Id}; создано назначение {newPrescription.Id}.");

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (
            MedicalRecordOperationResult.Success,
            new PrescriptionDto
            {
                Id = newPrescription.Id,
                Text = newPrescription.Text,
                IsActive = newPrescription.IsActive
            });
    }

    public async Task<List<MedicalRecordHistoryDto>?> GetHistoryAsync(int medicalRecordId)
    {
        if (!await MedicalRecordExistsAsync(medicalRecordId))
        {
            return null;
        }

        return await _db.MedicalRecordHistory
            .AsNoTracking()
            .Where(x => x.MedicalRecordId == medicalRecordId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new MedicalRecordHistoryDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                MedicalWorkerId = x.MedicalWorkerId,
                MedicalWorkerFullName = x.MedicalWorker.LastName + " " + x.MedicalWorker.FirstName,
                Action = x.Action,
                Data = x.Data
            })
            .ToListAsync();
    }

    private Task<bool> MedicalRecordExistsAsync(int medicalRecordId)
    {
        return _db.MedicalRecords.AnyAsync(x => x.Id == medicalRecordId);
    }

    private static MedicalRecordHistory CreateHistory(
        int medicalRecordId,
        int medicalWorkerId,
        MedicalRecordAction action,
        object data)
    {
        return new MedicalRecordHistory
        {
            MedicalRecordId = medicalRecordId,
            MedicalWorkerId = medicalWorkerId,
            CreatedAt = DateTime.UtcNow,
            Action = action,
            Data = JsonSerializer.Serialize(data, JsonOptions)
        };
    }

    private static MedicalRecordDto ToDto(MedicalRecord medicalRecord)
    {
        return new MedicalRecordDto
        {
            Id = medicalRecord.Id,
            PatientId = medicalRecord.PatientId,
            CreatedAt = medicalRecord.CreatedAt,
            Diagnoses = medicalRecord.Diagnoses
                .OrderBy(x => x.Diagnosis.Code)
                .Select(x => new MedicalRecordDiagnosisDto
                {
                    Id = x.Id,
                    DiagnosisId = x.DiagnosisId,
                    Code = x.Diagnosis.Code,
                    Name = x.Diagnosis.Name
                })
                .ToList(),
            Prescriptions = medicalRecord.Prescriptions
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.Id)
                .Select(x => new PrescriptionDto
                {
                    Id = x.Id,
                    Text = x.Text,
                    IsActive = x.IsActive
                })
                .ToList()
        };
    }
}
