using HIS.Api.Security;
using HIS.Api.Services;
using HIS.Contracts.MedicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.MedicalWorker)]
[Route("api/medical-records/{medicalRecordId:int}")]
public class MedicalRecordsController : ControllerBase
{
    private readonly MedicalRecordService _service;

    public MedicalRecordsController(MedicalRecordService service)
    {
        _service = service;
    }

    [HttpPost("diagnoses")]
    [Authorize(Policy = AuthorizationPolicies.Doctor)]
    public async Task<IActionResult> AddDiagnosis(
        int medicalRecordId,
        AddDiagnosisRequest request)
    {
        var result = await _service.AddDiagnosisAsync(
            medicalRecordId,
            request,
            User.GetMedicalWorkerId(),
            User.GetAccountId(),
            User.GetUsername());

        return ToActionResult(
            result,
            "Диагноз уже добавлен в ЭМК.");
    }

    [HttpDelete("diagnoses/{diagnosisId:int}")]
    [Authorize(Policy = AuthorizationPolicies.Doctor)]
    public async Task<IActionResult> RemoveDiagnosis(
        int medicalRecordId,
        int diagnosisId)
    {
        var result = await _service.RemoveDiagnosisAsync(
            medicalRecordId,
            diagnosisId,
            User.GetMedicalWorkerId(),
            User.GetAccountId(),
            User.GetUsername());

        return ToActionResult(result);
    }

    [HttpPost("prescriptions")]
    [Authorize(Policy = AuthorizationPolicies.Doctor)]
    public async Task<ActionResult<PrescriptionDto>> AddPrescription(
        int medicalRecordId,
        AddPrescriptionRequest request)
    {
        var result = await _service.AddPrescriptionAsync(
            medicalRecordId,
            request,
            User.GetMedicalWorkerId(),
            User.GetAccountId(),
            User.GetUsername());

        if (result.Result == MedicalRecordOperationResult.NotFound)
        {
            return NotFound();
        }

        return Ok(result.Prescription);
    }

    [HttpPost("prescriptions/{prescriptionId:int}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.Doctor)]
    public async Task<IActionResult> CancelPrescription(
        int medicalRecordId,
        int prescriptionId)
    {
        var result = await _service.CancelPrescriptionAsync(
            medicalRecordId,
            prescriptionId,
            User.GetMedicalWorkerId(),
            User.GetAccountId(),
            User.GetUsername());

        return ToActionResult(
            result,
            "Назначение уже неактивно.");
    }

    [HttpPut("prescriptions/{prescriptionId:int}")]
    [Authorize(Policy = AuthorizationPolicies.Doctor)]
    public async Task<ActionResult<PrescriptionDto>> EditPrescription(
        int medicalRecordId,
        int prescriptionId,
        EditPrescriptionRequest request)
    {
        var result = await _service.EditPrescriptionAsync(
            medicalRecordId,
            prescriptionId,
            request,
            User.GetMedicalWorkerId(),
            User.GetAccountId(),
            User.GetUsername());

        if (result.Result == MedicalRecordOperationResult.NotFound)
        {
            return NotFound();
        }

        if (result.Result == MedicalRecordOperationResult.Conflict)
        {
            return Conflict("Неактивное назначение нельзя редактировать.");
        }

        return Ok(result.Prescription);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<MedicalRecordHistoryDto>>> GetHistory(
        int medicalRecordId)
    {
        var history = await _service.GetHistoryAsync(medicalRecordId);

        if (history is null)
        {
            return NotFound();
        }

        return Ok(history);
    }

    private IActionResult ToActionResult(
        MedicalRecordOperationResult result,
        string conflictMessage = "Операция недоступна.")
    {
        return result switch
        {
            MedicalRecordOperationResult.NotFound => NotFound(),
            MedicalRecordOperationResult.Conflict => Conflict(conflictMessage),
            _ => NoContent()
        };
    }
}
