using HIS.Api.Services;
using HIS.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth/admin-setup")]
public class AdminSetupController : ControllerBase
{
    private readonly AdminSetupService _service;

    public AdminSetupController(AdminSetupService service)
    {
        _service = service;
    }

    [HttpGet("status")]
    public async Task<ActionResult<AdminSetupStatusDto>>
        GetStatus()
    {
        return Ok(await _service.GetStatusAsync());
    }

    [HttpPost("start")]
    public async Task<ActionResult<AdminSetupDto>> Start(
        StartAdminSetupRequest request)
    {
        var result = await _service.StartAsync(request);

        if (result.Result == AdminSetupOperationResult.AlreadyConfigured)
        {
            return Conflict("Администратор уже настроен.");
        }

        return Ok(result.Setup);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(
        ConfirmAdminSetupRequest request)
    {
        var result = await _service.ConfirmAsync(request);

        return result switch
        {
            AdminSetupOperationResult.NotStarted =>
                Conflict("Первоначальная настройка не начата."),

            AdminSetupOperationResult.AlreadyConfigured =>
                Conflict("Администратор уже настроен."),

            AdminSetupOperationResult.InvalidCode =>
                BadRequest("Неверный одноразовый код."),

            _ => NoContent()
        };
    }
}
