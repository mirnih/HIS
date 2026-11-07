using HIS.Api.Security;
using HIS.Api.Services;
using HIS.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AccountService _service;

    public AccountsController(AccountService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost("medical-workers/{medicalWorkerId:int}")]
    public async Task<ActionResult<AccountPasswordDto>> CreateForMedicalWorker(
        int medicalWorkerId,
        CreateMedicalWorkerAccountRequest request)
    {
        var result = await _service.CreateForMedicalWorkerAsync(
            medicalWorkerId,
            request,
            User.GetAccountId(),
            User.GetUsername());

        return result.Result switch
        {
            AccountOperationResult.NotFound => NotFound(),
            AccountOperationResult.Conflict => Conflict(
                "У медработника уже есть аккаунт или логин занят."),
            _ => Ok(result.Account)
        };
    }

    [HttpPost("{accountId:guid}/regenerate-password")]
    public async Task<ActionResult<AccountPasswordDto>> RegeneratePassword(
        Guid accountId)
    {
        var result = await _service.RegeneratePasswordAsync(
            accountId,
            User.GetAccountId(),
            User.GetUsername());

        return result.Result == AccountOperationResult.NotFound
            ? NotFound()
            : Ok(result.Account);
    }

    [HttpPut("{accountId:guid}/active")]
    public async Task<IActionResult> SetActive(
        Guid accountId,
        SetAccountActiveRequest request)
    {
        var result = await _service.SetActiveAsync(
            accountId,
            request.IsActive,
            User.GetAccountId(),
            User.GetUsername());

        return result switch
        {
            AccountOperationResult.NotFound => NotFound(),
            AccountOperationResult.AdministratorProtected => Conflict(
                "Аккаунт единственного администратора нельзя деактивировать."),
            _ => NoContent()
        };
    }
}
