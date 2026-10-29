using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HIS.Api.Services;
using HIS.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HIS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _service;

    public AuthController(AuthService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenPairDto>> Login(
        LoginRequest request)
    {
        var result = await _service.LoginAsync(request);

        return result.Result switch
        {
            AuthOperationResult.TotpRequired => Unauthorized(new
            {
                Message = "Требуется одноразовый код.",
                TotpRequired = true
            }),
            AuthOperationResult.InvalidCredentials =>
                Unauthorized("Неверные данные для входа."),
            _ => Ok(result.Tokens)
        };
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenPairDto>> Refresh(
        RefreshTokenRequest request)
    {
        var result = await _service.RefreshAsync(request);

        if (result.Result == AuthOperationResult.InvalidRefreshToken)
        {
            return Unauthorized("Refresh token недействителен.");
        }

        return Ok(result.Tokens);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request)
    {
        await _service.LogoutAsync(request);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUserDto> GetCurrentUser()
    {
        var accountIdValue = User.FindFirstValue(
            JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(accountIdValue, out var accountId))
        {
            return Unauthorized();
        }

        int? medicalWorkerId = null;
        var medicalWorkerIdValue = User.FindFirstValue(
            "medical_worker_id");

        if (int.TryParse(
            medicalWorkerIdValue,
            out var parsedMedicalWorkerId))
        {
            medicalWorkerId = parsedMedicalWorkerId;
        }

        return Ok(new CurrentUserDto
        {
            AccountId = accountId,
            Username = User.FindFirstValue(
                JwtRegisteredClaimNames.UniqueName) ?? string.Empty,
            Role = User.FindFirstValue("role") ?? string.Empty,
            MedicalWorkerId = medicalWorkerId
        });
    }
}
