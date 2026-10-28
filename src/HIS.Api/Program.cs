using HIS.Api.Data;
using HIS.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<HisDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy =>
    {
        policy 
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<MedicalWorkerService>();
builder.Services.AddScoped<DiagnosisService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<MedicalRecordService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
var app = builder.Build();

app.UseCors("Client");

app.MapControllers();

app.Run();
