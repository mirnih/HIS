using HIS.Api.Services;
using HIS.Contracts.MedicalRecords;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Route("api/patients/{patientId:int}/medical-record")]
public class PatientMedicalRecordsController : ControllerBase
{
    private readonly MedicalRecordService _service;

    public PatientMedicalRecordsController(MedicalRecordService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<MedicalRecordDto>> GetByPatientId(int patientId)
    {
        var medicalRecord = await _service.GetByPatientIdAsync(patientId);

        if (medicalRecord is null)
        {
            return NotFound();
        }

        return Ok(medicalRecord);
    }

    [HttpPost]
    public async Task<ActionResult<MedicalRecordDto>> Create(int patientId)
    {
        var result = await _service.CreateAsync(patientId);

        return result.Result switch
        {
            MedicalRecordOperationResult.NotFound => NotFound(),
            MedicalRecordOperationResult.Conflict => Conflict("У пациента уже существует ЭМК."),
            _ => CreatedAtAction(
                nameof(GetByPatientId),
                new { patientId },
                result.MedicalRecord)
        };
    }
}
