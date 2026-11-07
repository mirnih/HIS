using HIS.Api.Security;
using HIS.Api.Services;
using HIS.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.MedicalWorker)]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly PatientService _service;

    public PatientsController(PatientService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<PatientDto>>> GetAll()
    {
        var patients = await _service.GetAllAsync();

        return Ok(patients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDto>> GetById(int id)
    {
        var patient = await _service.GetByIdAsync(id);

        if (patient is null)
        {
            return NotFound();
        }

        return Ok(patient);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.DoctorOrNurse)]
    public async Task<ActionResult<PatientDto>> Create(
        CreatePatientRequest request)
    {
        var patient = await _service.CreateAsync(
            request,
            User.GetAccountId(),
            User.GetUsername());

        return CreatedAtAction(
            nameof(GetById),
            new { id = patient.Id },
            patient);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdatePatientRequest request)
    {
        var updated = await _service.UpdateAsync(
            id,
            request,
            User.GetAccountId(),
            User.GetUsername());

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(
            id,
            User.GetAccountId(),
            User.GetUsername());

        return result switch
        {
            PatientDeleteResult.NotFound =>
                NotFound(),

            PatientDeleteResult.HasMedicalRecord =>
                Conflict(
                    "Нельзя удалить пациента, для которого существует ЭМК."),

            _ => NoContent()
        };
    }
}
