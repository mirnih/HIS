using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Authentication;
using System.Security.Cryptography;
using HIS.Api.Data;
using HIS.Api.Models;
using HIS.Api.Options;
using HIS.Api.Security;
using HIS.Api.Services;
using HIS.Contracts.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = OperatingSystem.IsMacOS()
            ? SslProtocols.Tls12
            : SslProtocols.Tls13;
    });
});

// Add services to the container.

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are missing.");

var publicKeyPath = Path.Combine(
    builder.Environment.ContentRootPath,
    jwtOptions.PublicKeyPath);

if (!File.Exists(publicKeyPath))
{
    throw new InvalidOperationException(
        $"JWT public key was not found: {publicKeyPath}");
}

var publicRsa = RSA.Create();
publicRsa.ImportFromPem(File.ReadAllText(publicKeyPath));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(publicRsa),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var accountIdValue = context.Principal?.FindFirstValue(
                    JwtRegisteredClaimNames.Sub);

                if (!Guid.TryParse(accountIdValue, out var accountId))
                {
                    context.Fail("Account identifier is missing.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<HisDbContext>();

                var account = await db.Accounts
                    .SingleOrDefaultAsync(x => x.Id == accountId);

                if (account is null || !account.IsActive)
                {
                    context.Fail("Account is inactive.");
                    return;
                }

                var role = context.Principal?.FindFirstValue("role");
                var accountType = context.Principal?.FindFirstValue(
                    "account_type");

                if (account is AdministratorAccount administrator)
                {
                    if (!administrator.IsTotpConfigured ||
                        role != "Administrator" ||
                        accountType != "administrator")
                    {
                        context.Fail("Administrator token is no longer valid.");
                    }

                    return;
                }

                if (account is MedicalWorkerAccount medicalAccount)
                {
                    await db.Entry(medicalAccount)
                        .Reference(x => x.MedicalWorker)
                        .LoadAsync();

                    if (medicalAccount.MedicalWorker.IsDeleted ||
                        role != medicalAccount.MedicalWorker.Role.ToString() ||
                        accountType != "medical_worker")
                    {
                        context.Fail("Medical worker token is no longer valid.");
                    }

                    return;
                }

                context.Fail("Unsupported account type.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(
        AuthorizationPolicies.Administrator,
        policy => policy.RequireRole("Administrator"));

    options.AddPolicy(
        AuthorizationPolicies.MedicalWorker,
        policy => policy.RequireClaim(
            "account_type",
            "medical_worker"));

    options.AddPolicy(
        AuthorizationPolicies.Doctor,
        policy => policy
            .RequireClaim("account_type", "medical_worker")
            .RequireRole(MedicalWorkerRole.Doctor.ToString()));

    options.AddPolicy(
        AuthorizationPolicies.DoctorOrNurse,
        policy => policy
            .RequireClaim("account_type", "medical_worker")
            .RequireRole(
                MedicalWorkerRole.Doctor.ToString(),
                MedicalWorkerRole.Nurse.ToString()));

    options.AddPolicy(
        AuthorizationPolicies.Authenticated,
        policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddControllers();

builder.Services.AddDbContext<HisDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy =>
    {
        policy
            .WithOrigins("https://localhost:7252")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<MedicalWorkerService>();
builder.Services.AddScoped<DiagnosisService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<MedicalRecordService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdminSetupService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<AccountService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
var app = builder.Build();

app.UseCors("Client");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
