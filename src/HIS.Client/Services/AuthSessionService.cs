using System.Text;
using System.Text.Json;
using HIS.Contracts.Auth;
using Microsoft.JSInterop;

namespace HIS.Client.Services;

public class AuthSessionService
{
    private const string StorageKey = "his-auth-session";
    private readonly IJSRuntime _js;
    private bool _initialized;

    public AuthSessionService(IJSRuntime js)
    {
        _js = js;
    }

    public TokenPairDto? Tokens { get; private set; }
    public CurrentUserDto? User { get; private set; }
    public bool IsAuthenticated => Tokens is not null && User is not null;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var json = await _js.InvokeAsync<string?>(
            "sessionStorage.getItem",
            StorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            Tokens = JsonSerializer.Deserialize<TokenPairDto>(json);

            if (Tokens is null ||
                Tokens.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                await ClearAsync();
                return;
            }

            User = ReadUser(Tokens.AccessToken);
        }
        catch
        {
            await ClearAsync();
        }
    }

    public async Task SetTokensAsync(TokenPairDto tokens)
    {
        Tokens = tokens;
        User = ReadUser(tokens.AccessToken);
        _initialized = true;

        await _js.InvokeVoidAsync(
            "sessionStorage.setItem",
            StorageKey,
            JsonSerializer.Serialize(tokens));
    }

    public async Task ClearAsync()
    {
        Tokens = null;
        User = null;
        _initialized = true;

        await _js.InvokeVoidAsync(
            "sessionStorage.removeItem",
            StorageKey);
    }

    private static CurrentUserDto ReadUser(string jwt)
    {
        var parts = jwt.Split('.');

        if (parts.Length != 3)
        {
            throw new InvalidOperationException("JWT has an invalid format.");
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(
            payload.Length + (4 - payload.Length % 4) % 4,
            '=');

        using var document = JsonDocument.Parse(
            Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        var root = document.RootElement;

        var result = new CurrentUserDto
        {
            AccountId = Guid.Parse(root.GetProperty("sub").GetString()!),
            Username = root.GetProperty("unique_name").GetString()
                ?? string.Empty,
            Role = root.GetProperty("role").GetString()
                ?? string.Empty
        };

        if (root.TryGetProperty(
            "medical_worker_id",
            out var medicalWorkerProperty) &&
            int.TryParse(
                medicalWorkerProperty.GetString(),
                out var medicalWorkerId))
        {
            result.MedicalWorkerId = medicalWorkerId;
        }

        return result;
    }
}
