using HIS.Api.Security;
using HIS.Api.Services;
using HIS.Contracts.MedicalWorkers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/medical-workers")]
public class MedicalWorkersController : ControllerBase
{
    private readonly MedicalWorkerService _service;

    public MedicalWorkersController(MedicalWorkerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<MedicalWorkerDto>>> GetAll()
    {
        var workers = await _service.GetAllAsync();
        return Ok(workers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MedicalWorkerDto>> GetById(int id)
    {
        var worker = await _service.GetByIdAsync(id);
        if (worker is null)
        {
            return NotFound();
        }
        return Ok(worker);
    }

    [HttpPost]
    public async Task<ActionResult<MedicalWorkerDto>> Create(CreateMedicalWorkerRequest request)
    {
        var worker = await _service.CreateAsync(
            request,
            User.GetAccountId(),
            User.GetUsername());
        return CreatedAtAction(
            nameof(GetById),
            new { id = worker.Id },
            worker);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateMedicalWorkerRequest request)
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
        var deleted = await _service.DeleteAsync(
            id,
            User.GetAccountId(),
            User.GetUsername());
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(
        int id,
        UpdateMedicalWorkerRoleRequest request)
    {
        var updated = await _service.UpdateRoleAsync(
            id,
            request,
            User.GetAccountId(),
            User.GetUsername());

        return updated ? NoContent() : NotFound();
    }
}
