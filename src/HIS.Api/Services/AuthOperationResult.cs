namespace HIS.Api.Services;

public enum AuthOperationResult
{
    Success,
    InvalidCredentials,
    TotpRequired,
    InvalidRefreshToken
}
