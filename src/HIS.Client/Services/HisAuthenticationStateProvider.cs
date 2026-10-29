using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace HIS.Client.Services;

public class HisAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthSessionService _session;

    public HisAuthenticationStateProvider(AuthSessionService session)
    {
        _session = session;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _session.InitializeAsync();
        return BuildState();
    }

    public void NotifyUserChanged()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    private AuthenticationState BuildState()
    {
        if (!_session.IsAuthenticated)
        {
            return new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var user = _session.User!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.AccountId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.MedicalWorkerId.HasValue)
        {
            claims.Add(new Claim(
                "medical_worker_id",
                user.MedicalWorkerId.Value.ToString()));
        }

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
    }
}
