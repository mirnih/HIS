using HIS.Api.Security;
using HIS.Api.Services;
using HIS.Contracts.Diagnoses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/diagnoses")]
public class DiagnosesController : ControllerBase
{
    private readonly DiagnosisService _service;

    public DiagnosesController(DiagnosisService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<DiagnosisDto>>> GetAll()
    {
        var diagnoses = await _service.GetAllAsync();

        return Ok(diagnoses);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DiagnosisDto>> GetById(int id)
    {
        var diagnosis = await _service.GetByIdAsync(id);

        if (diagnosis is null)
        {
            return NotFound();
        }

        return Ok(diagnosis);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public async Task<ActionResult<DiagnosisDto>> Create(
        CreateDiagnosisRequest request)
    {
        var result = await _service.CreateAsync(request);

        if (result.Result == DiagnosisOperationResult.Conflict)
        {
            return Conflict(
                "Диагноз с таким кодом или наименованием уже существует.");
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Diagnosis!.Id },
            result.Diagnosis);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public async Task<IActionResult> Update(
        int id,
        UpdateDiagnosisRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        return result switch
        {
            DiagnosisOperationResult.NotFound =>
                NotFound(),

            DiagnosisOperationResult.Conflict =>
                Conflict(
                    "Диагноз с таким кодом или наименованием уже существует."),

            _ => NoContent()
        };
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);

        return result switch
        {
            DiagnosisOperationResult.NotFound =>
                NotFound(),

            DiagnosisOperationResult.InUse =>
                Conflict(
                    "Диагноз используется в ЭМК и не может быть удален."),

            _ => NoContent()
        };
    }

    [HttpPost("import")]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("Файл пуст.");
        }

        await using var stream = file.OpenReadStream();

        var imported = await _service.ImportAsync(stream);

        return Ok(new
        {
            Imported = imported
        });
    }
}
