using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Contracts.Auth;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace HIS.Api.Services;

public class AdminSetupService
{
    private const string AdminUsername = "admin";

    private readonly HisDbContext _db;
    private readonly AuditService _auditService;

    public AdminSetupService(
        HisDbContext db,
        AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task<AdminSetupStatusDto> GetStatusAsync()
    {
        var administrator = await _db.AdministratorAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync();

        return new AdminSetupStatusDto
        {
            IsConfigured =
                administrator is not null &&
                administrator.IsActive &&
                administrator.IsTotpConfigured &&
                !string.IsNullOrWhiteSpace(
                    administrator.PasswordHash)
        };
    }

    public async Task<(AdminSetupOperationResult Result, AdminSetupDto? Setup)> StartAsync(StartAdminSetupRequest request)
    {
        var administrator = await _db.AdministratorAccounts.SingleOrDefaultAsync();

        if (administrator?.IsTotpConfigured == true)
        {
            return (AdminSetupOperationResult.AlreadyConfigured, null);
        }

        var now = DateTime.UtcNow;
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        if (administrator is null)
        {
            administrator = new AdministratorAccount
            {
                Id = Guid.NewGuid(),
                Username = AdminUsername,
                PasswordHash = passwordHash,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = false,
                TotpSecret = secret,
                IsTotpConfigured = false
            };

            _db.AdministratorAccounts.Add(administrator);
        }
        else
        {
            administrator.PasswordHash = passwordHash;
            administrator.TotpSecret = secret;
            administrator.UpdatedAt = now;
            administrator.IsActive = false;
            administrator.IsTotpConfigured = false;
        }

        _auditService.AddEvent(
            administrator.Id,
            "initial-admin-setup",
            "Начата первоначальная настройка администратора.");

        await _db.SaveChangesAsync();

        var issuer = Uri.EscapeDataString("HIS");
        var label = Uri.EscapeDataString($"{AdminUsername}@HIS");

        var otpAuthUri =
            $"otpauth://totp/{label}" +
            $"?secret={secret}" +
            $"&issuer={issuer}" +
            "&digits=6&period=30";

        return (
            AdminSetupOperationResult.Success,
            new AdminSetupDto
            {
                ManualKey = secret,
                OtpAuthUri = otpAuthUri
            });
    }

    public async Task<AdminSetupOperationResult> ConfirmAsync(
        ConfirmAdminSetupRequest request)
    {
        var administrator = await _db.AdministratorAccounts
            .SingleOrDefaultAsync();

        if (administrator is null || string.IsNullOrWhiteSpace(administrator.PasswordHash) || string.IsNullOrWhiteSpace(administrator.TotpSecret))
        {
            return AdminSetupOperationResult.NotStarted;
        }

        if (administrator.IsTotpConfigured)
        {
            return AdminSetupOperationResult.AlreadyConfigured;
        }

        var secretBytes = Base32Encoding.ToBytes(administrator.TotpSecret);

        var totp = new Totp(
            secretBytes,
            step: 30,
            mode: OtpHashMode.Sha1,
            totpSize: 6);

        var isValid = totp.VerifyTotp(
            request.Code,
            out _,
            new VerificationWindow(previous: 1, future: 1)
            );

        if (!isValid)
        {
            return AdminSetupOperationResult.InvalidCode;
        }

        administrator.IsTotpConfigured = true;
        administrator.IsActive = true;
        administrator.UpdatedAt = DateTime.UtcNow;

        _auditService.AddEvent(
            administrator.Id,
            administrator.Username,
            "Первоначальная настройка администратора завершена.");

        await _db.SaveChangesAsync();
        return AdminSetupOperationResult.Success;
    }
}