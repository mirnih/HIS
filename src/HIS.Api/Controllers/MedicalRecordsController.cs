using HIS.Api.Services;
using HIS.Contracts.MedicalRecords;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Route("api/medical-records/{medicalRecordId:int}")]
public class MedicalRecordsController : ControllerBase
{
    private readonly MedicalRecordService _service;

    public MedicalRecordsController(MedicalRecordService service)
    {
        _service = service;
    }

    [HttpPost("diagnoses")]
    public async Task<IActionResult> AddDiagnosis(
        int medicalRecordId,
        AddDiagnosisRequest request)
    {
        var result = await _service.AddDiagnosisAsync(medicalRecordId, request);

        return ToActionResult(
            result,
            "Диагноз уже добавлен в ЭМК.");
    }

    [HttpDelete("diagnoses/{diagnosisId:int}")]
    public async Task<IActionResult> RemoveDiagnosis(
        int medicalRecordId,
        int diagnosisId,
        RemoveDiagnosisRequest request)
    {
        var result = await _service.RemoveDiagnosisAsync(
            medicalRecordId,
            diagnosisId,
            request);

        return ToActionResult(result);
    }

    [HttpPost("prescriptions")]
    public async Task<ActionResult<PrescriptionDto>> AddPrescription(
        int medicalRecordId,
        AddPrescriptionRequest request)
    {
        var result = await _service.AddPrescriptionAsync(
            medicalRecordId,
            request);

        if (result.Result == MedicalRecordOperationResult.NotFound)
        {
            return NotFound();
        }

        if (result.Result == MedicalRecordOperationResult.InvalidMedicalWorker)
        {
            return BadRequest("Необходимо указать действующего медработника.");
        }

        return Ok(result.Prescription);
    }

    [HttpPost("prescriptions/{prescriptionId:int}/cancel")]
    public async Task<IActionResult> CancelPrescription(
        int medicalRecordId,
        int prescriptionId,
        CancelPrescriptionRequest request)
    {
        var result = await _service.CancelPrescriptionAsync(
            medicalRecordId,
            prescriptionId,
            request);

        return ToActionResult(
            result,
            "Назначение уже неактивно.");
    }

    [HttpPut("prescriptions/{prescriptionId:int}")]
    public async Task<ActionResult<PrescriptionDto>> EditPrescription(
        int medicalRecordId,
        int prescriptionId,
        EditPrescriptionRequest request)
    {
        var result = await _service.EditPrescriptionAsync(
            medicalRecordId,
            prescriptionId,
            request);

        if (result.Result == MedicalRecordOperationResult.NotFound)
        {
            return NotFound();
        }

        if (result.Result == MedicalRecordOperationResult.InvalidMedicalWorker)
        {
            return BadRequest("Необходимо указать действующего медработника.");
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
            MedicalRecordOperationResult.InvalidMedicalWorker =>
                BadRequest("Необходимо указать действующего медработника."),
            MedicalRecordOperationResult.Conflict => Conflict(conflictMessage),
            _ => NoContent()
        };
    }
}
