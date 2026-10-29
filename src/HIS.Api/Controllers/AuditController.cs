using HIS.Api.Security;
using HIS.Api.Services;
using HIS.Contracts.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly AuditService _service;

    public AuditController(AuditService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditEventDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }
}
