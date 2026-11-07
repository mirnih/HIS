using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.Auth;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace HIS.Api.Services;

public class AuthService
{
    private readonly HisDbContext _db;
    private readonly TokenService _tokenService;
    private readonly AuditService _auditService;

    public AuthService(
        HisDbContext db,
        TokenService tokenService,
        AuditService auditService)
    {
        _db = db;
        _tokenService = tokenService;
        _auditService = auditService;
    }

    public async Task<(AuthOperationResult Result, TokenPairDto? Tokens)>
        LoginAsync(LoginRequest request)
    {
        var username = request.Username.Trim();
        var account = await _db.Accounts
            .SingleOrDefaultAsync(x => x.Username == username);

        if (account is null)
        {
            return await FailedLoginAsync(
                null,
                username,
                "Неуспешная попытка входа: аккаунт не найден.");
        }

        if (!account.IsActive ||
            string.IsNullOrWhiteSpace(account.PasswordHash))
        {
            return await FailedLoginAsync(
                account.Id,
                account.Username,
                "Неуспешная попытка входа: аккаунт неактивен.");
        }

        if (!BCrypt.Net.BCrypt.Verify(
            request.Password,
            account.PasswordHash))
        {
            return await FailedLoginAsync(
                account.Id,
                account.Username,
                "Неуспешная попытка входа: неверный пароль.");
        }

        if (account is AdministratorAccount administrator)
        {
            if (string.IsNullOrWhiteSpace(request.TotpCode))
            {
                _auditService.AddEvent(
                    administrator.Id,
                    administrator.Username,
                    "Неуспешная попытка входа администратора: " +
                    "не указан TOTP-код.");
                await _db.SaveChangesAsync();

                return (AuthOperationResult.TotpRequired, null);
            }

            if (!VerifyTotp(administrator, request.TotpCode))
            {
                return await FailedLoginAsync(
                    administrator.Id,
                    administrator.Username,
                    "Неуспешная попытка входа администратора: " +
                    "неверный TOTP-код.");
            }
        }
        else if (account is MedicalWorkerAccount medicalAccount)
        {
            await _db.Entry(medicalAccount)
                .Reference(x => x.MedicalWorker)
                .LoadAsync();

            if (medicalAccount.MedicalWorker.IsDeleted)
            {
                return await FailedLoginAsync(
                    account.Id,
                    account.Username,
                    "Неуспешная попытка входа: медработник удалён.");
            }
        }
        else
        {
            return await FailedLoginAsync(
                account.Id,
                account.Username,
                "Неуспешная попытка входа: неподдерживаемый тип аккаунта.");
        }

        var tokens = CreateTokenPair(account);

        _auditService.AddEvent(
            account.Id,
            account.Username,
            account is AdministratorAccount
                ? "Успешный вход администратора."
                : "Успешный вход пользователя.");

        await _db.SaveChangesAsync();

        return (AuthOperationResult.Success, tokens);
    }

    public async Task<(AuthOperationResult Result, TokenPairDto? Tokens)>
        RefreshAsync(RefreshTokenRequest request)
    {
        var hash = _tokenService.HashRefreshToken(
            request.RefreshToken);

        var storedToken = await _db.RefreshTokens
            .Include(x => x.Account)
            .SingleOrDefaultAsync(x => x.TokenHash == hash);

        if (storedToken is null ||
            storedToken.RevokedAt is not null ||
            storedToken.ExpiresAt <= DateTime.UtcNow ||
            !storedToken.Account.IsActive)
        {
            return (AuthOperationResult.InvalidRefreshToken, null);
        }

        if (storedToken.Account is AdministratorAccount administrator)
        {
            if (!administrator.IsTotpConfigured)
            {
                return (AuthOperationResult.InvalidRefreshToken, null);
            }
        }
        else if (storedToken.Account is MedicalWorkerAccount medicalAccount)
        {
            await _db.Entry(medicalAccount)
                .Reference(x => x.MedicalWorker)
                .LoadAsync();

            if (medicalAccount.MedicalWorker.IsDeleted)
            {
                return (AuthOperationResult.InvalidRefreshToken, null);
            }
        }
        else
        {
            return (AuthOperationResult.InvalidRefreshToken, null);
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        var tokens = CreateTokenPair(storedToken.Account);

        await _db.SaveChangesAsync();

        return (AuthOperationResult.Success, tokens);
    }

    public async Task LogoutAsync(RefreshTokenRequest request)
    {
        var hash = _tokenService.HashRefreshToken(
            request.RefreshToken);

        var storedToken = await _db.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == hash);

        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private TokenPairDto CreateTokenPair(Account account)
    {
        var accessToken = _tokenService.CreateAccessToken(account);
        var refreshToken = _tokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            TokenHash = refreshToken.Hash,
            CreatedAt = refreshToken.CreatedAt,
            ExpiresAt = refreshToken.ExpiresAt
        });

        return new TokenPairDto
        {
            AccessToken = accessToken.Value,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Value,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        };
    }

    private async Task<(AuthOperationResult Result, TokenPairDto? Tokens)>
        FailedLoginAsync(
            Guid? accountId,
            string actor,
            string message)
    {
        _auditService.AddEvent(accountId, actor, message);
        await _db.SaveChangesAsync();

        return (AuthOperationResult.InvalidCredentials, null);
    }

    private static bool VerifyTotp(
        AdministratorAccount administrator,
        string code)
    {
        if (!administrator.IsTotpConfigured ||
            string.IsNullOrWhiteSpace(administrator.TotpSecret))
        {
            return false;
        }

        var secretBytes = Base32Encoding.ToBytes(
            administrator.TotpSecret);

        var totp = new Totp(
            secretBytes,
            step: 30,
            mode: OtpHashMode.Sha1,
            totpSize: 6);

        return totp.VerifyTotp(
            code,
            out _,
            new VerificationWindow(previous: 1, future: 1));
    }
}
