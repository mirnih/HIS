using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HIS.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetAccountId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(value, out var accountId))
        {
            throw new InvalidOperationException(
                "Access token does not contain a valid account identifier.");
        }

        return accountId;
    }

    public static int GetMedicalWorkerId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("medical_worker_id");

        if (!int.TryParse(value, out var medicalWorkerId))
        {
            throw new InvalidOperationException(
                "Access token does not contain a valid medical worker identifier.");
        }

        return medicalWorkerId;
    }

    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? throw new InvalidOperationException(
                "Access token does not contain a username.");
    }
}
