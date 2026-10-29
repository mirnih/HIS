using System.Security.Cryptography;
using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.Accounts;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class AccountService
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string PasswordAlphabet = Uppercase + Lowercase + Digits;

    private readonly HisDbContext _db;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly AuditService _auditService;

    public AccountService(
        HisDbContext db,
        RefreshTokenService refreshTokenService,
        AuditService auditService)
    {
        _db = db;
        _refreshTokenService = refreshTokenService;
        _auditService = auditService;
    }

    public async Task<List<AccountDto>> GetAllAsync()
    {
        var administrators = await _db.AdministratorAccounts
            .AsNoTracking()
            .Select(x => new AccountDto
            {
                Id = x.Id,
                Username = x.Username,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive,
                IsAdministrator = true
            })
            .ToListAsync();

        var medicalAccounts = await _db.MedicalWorkerAccounts
            .AsNoTracking()
            .Include(x => x.MedicalWorker)
            .Select(x => new AccountDto
            {
                Id = x.Id,
                Username = x.Username,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive,
                IsAdministrator = false,
                MedicalWorkerId = x.MedicalWorkerId,
                MedicalWorkerFullName =
                    x.MedicalWorker.LastName + " " +
                    x.MedicalWorker.FirstName,
                MedicalWorkerRole = x.MedicalWorker.Role
            })
            .ToListAsync();

        return administrators
            .Concat(medicalAccounts)
            .OrderBy(x => x.Username)
            .ToList();
    }

    public async Task<(
        AccountOperationResult Result,
        AccountPasswordDto? Account)> CreateForMedicalWorkerAsync(
            int medicalWorkerId,
            CreateMedicalWorkerAccountRequest request,
            Guid actorAccountId,
            string actor)
    {
        var worker = await _db.MedicalWorkers
            .Include(x => x.Account)
            .SingleOrDefaultAsync(x => x.Id == medicalWorkerId);

        if (worker is null || worker.IsDeleted)
        {
            return (AccountOperationResult.NotFound, null);
        }

        var username = request.Username.Trim();

        if (worker.Account is not null ||
            await _db.Accounts.AnyAsync(x => x.Username == username))
        {
            return (AccountOperationResult.Conflict, null);
        }

        var now = DateTime.UtcNow;
        var password = GeneratePassword();
        var account = new MedicalWorkerAccount
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                password,
                workFactor: 12),
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true,
            MedicalWorkerId = worker.Id
        };

        _db.MedicalWorkerAccounts.Add(account);
        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Создан аккаунт {username} для медработника " +
            $"{worker.LastName} {worker.FirstName}.");

        await _db.SaveChangesAsync();

        return (
            AccountOperationResult.Success,
            new AccountPasswordDto
            {
                Account = ToDto(account, worker),
                GeneratedPassword = password
            });
    }

    public async Task<(
        AccountOperationResult Result,
        AccountPasswordDto? Account)> RegeneratePasswordAsync(
            Guid accountId,
            Guid actorAccountId,
            string actor)
    {
        var account = await _db.MedicalWorkerAccounts
            .Include(x => x.MedicalWorker)
            .SingleOrDefaultAsync(x => x.Id == accountId);

        if (account is null)
        {
            return (AccountOperationResult.NotFound, null);
        }

        var password = GeneratePassword();
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
            password,
            workFactor: 12);
        account.UpdatedAt = DateTime.UtcNow;

        var revokedCount = await _refreshTokenService
            .RevokeAllAsync(account.Id);

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Сгенерирован новый пароль аккаунта {account.Username}; " +
            $"отозвано refresh tokens: {revokedCount}.");

        await _db.SaveChangesAsync();

        return (
            AccountOperationResult.Success,
            new AccountPasswordDto
            {
                Account = ToDto(account, account.MedicalWorker),
                GeneratedPassword = password
            });
    }

    public async Task<AccountOperationResult> SetActiveAsync(
        Guid accountId,
        bool isActive,
        Guid actorAccountId,
        string actor)
    {
        var account = await _db.Accounts
            .SingleOrDefaultAsync(x => x.Id == accountId);

        if (account is null)
        {
            return AccountOperationResult.NotFound;
        }

        if (account is AdministratorAccount)
        {
            return AccountOperationResult.AdministratorProtected;
        }

        account.IsActive = isActive;
        account.UpdatedAt = DateTime.UtcNow;

        var revokedCount = isActive
            ? 0
            : await _refreshTokenService.RevokeAllAsync(account.Id);

        _auditService.AddEvent(
            actorAccountId,
            actor,
            $"Аккаунт {account.Username} " +
            $"{(isActive ? "активирован" : "деактивирован")}; " +
            $"отозвано refresh tokens: {revokedCount}.");

        await _db.SaveChangesAsync();

        return AccountOperationResult.Success;
    }

    private static string GeneratePassword()
    {
        var password = new char[12];
        password[0] = Uppercase[RandomNumberGenerator.GetInt32(
            Uppercase.Length)];
        password[1] = Lowercase[RandomNumberGenerator.GetInt32(
            Lowercase.Length)];
        password[2] = Digits[RandomNumberGenerator.GetInt32(
            Digits.Length)];

        for (var index = 3; index < password.Length; index++)
        {
            password[index] = PasswordAlphabet[
                RandomNumberGenerator.GetInt32(
                    PasswordAlphabet.Length)];
        }

        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) =
                (password[swapIndex], password[index]);
        }

        return new string(password);
    }

    private static AccountDto ToDto(
        MedicalWorkerAccount account,
        MedicalWorker worker)
    {
        return new AccountDto
        {
            Id = account.Id,
            Username = account.Username,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            IsActive = account.IsActive,
            IsAdministrator = false,
            MedicalWorkerId = worker.Id,
            MedicalWorkerFullName =
                worker.LastName + " " + worker.FirstName,
            MedicalWorkerRole = worker.Role
        };
    }
}
