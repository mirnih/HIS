using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HIS.Api.Models;
using HIS.Api.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HIS.Api.Services;

public sealed record GeneratedAccessToken(string Value, DateTime ExpiresAt);

public sealed record GeneratedRefreshToken(
    string Value,
    string Hash,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public class TokenService : IDisposable
{
    private readonly JwtOptions _options;
    private readonly RSA _privateRsa;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(
        IOptions<JwtOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;

        var privateKeyPath = Path.Combine(
            environment.ContentRootPath,
            _options.PrivateKeyPath);

        if (!File.Exists(privateKeyPath))
        {
            throw new InvalidOperationException(
                $"JWT private key was not found: {privateKeyPath}");
        }

        _privateRsa = RSA.Create();

        var privateKeyPem = File.ReadAllText(privateKeyPath);
        _privateRsa.ImportFromPem(privateKeyPem);

        var securityKey = new RsaSecurityKey(_privateRsa);

        _signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.RsaSha256);
    }

    public GeneratedAccessToken CreateAccessToken(Account account)
    {
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddMinutes(
            _options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                account.Id.ToString()),

            new(
                JwtRegisteredClaimNames.UniqueName,
                account.Username),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(createdAt)
                    .ToUnixTimeSeconds()
                    .ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        switch (account)
        {
            case AdministratorAccount:
                claims.Add(new Claim(
                    "account_type",
                    "administrator"));

                claims.Add(new Claim(
                    "role",
                    "Administrator"));

                break;

            case MedicalWorkerAccount medicalAccount:
                if (medicalAccount.MedicalWorker is null)
                {
                    throw new InvalidOperationException(
                        "Medical worker account must include MedicalWorker.");
                }

                claims.Add(new Claim(
                    "account_type",
                    "medical_worker"));

                claims.Add(new Claim(
                    "medical_worker_id",
                    medicalAccount.MedicalWorkerId.ToString(
                        CultureInfo.InvariantCulture)));

                claims.Add(new Claim(
                    "role",
                    medicalAccount.MedicalWorker.Role.ToString()));

                break;

            default:
                throw new InvalidOperationException(
                    "Unsupported account type.");
        }

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: createdAt,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        var handler = new JwtSecurityTokenHandler();

        return new GeneratedAccessToken(
            handler.WriteToken(jwt),
            expiresAt);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddDays(
            _options.RefreshTokenLifetimeDays);

        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var value = WebEncoders.Base64UrlEncode(randomBytes);
        var hash = HashRefreshToken(value);

        return new GeneratedRefreshToken(
            value,
            hash,
            createdAt,
            expiresAt);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    public void Dispose()
    {
        _privateRsa.Dispose();
        GC.SuppressFinalize(this);
    }
}