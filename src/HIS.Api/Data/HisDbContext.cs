using HIS.Api.Models;
using HIS.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Data;

public class HisDbContext : DbContext
{
    public HisDbContext(DbContextOptions<HisDbContext> options) : base(options){}

    public DbSet<MedicalWorker> MedicalWorkers => Set<MedicalWorker>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<MedicalRecordDiagnosis> MedicalRecordDiagnoses => Set<MedicalRecordDiagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<MedicalRecordHistory> MedicalRecordHistory => Set<MedicalRecordHistory>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AdministratorAccount> AdministratorAccounts => Set<AdministratorAccount>();
    public DbSet<MedicalWorkerAccount> MedicalWorkerAccounts => Set<MedicalWorkerAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Diagnosis>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Diagnosis>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<MedicalRecord>()
            .HasIndex(x => x.PatientId)
            .IsUnique();

        modelBuilder.Entity<MedicalRecord>()
            .HasOne(x => x.Patient)
            .WithOne(x => x.MedicalRecord)
            .HasForeignKey<MedicalRecord>(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicalRecordDiagnosis>()
            .HasOne(x => x.MedicalRecord)
            .WithMany(x => x.Diagnoses)
            .HasForeignKey(x => x.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecordDiagnosis>()
            .HasOne(x => x.Diagnosis)
            .WithMany()
            .HasForeignKey(x => x.DiagnosisId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicalRecordDiagnosis>()
            .HasIndex(x => new
            {
                x.MedicalRecordId,
                x.DiagnosisId
            })
            .IsUnique();

        modelBuilder.Entity<Prescription>()
            .HasOne(x => x.MedicalRecord)
            .WithMany(x => x.Prescriptions)
            .HasForeignKey(x => x.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecordHistory>()
            .HasOne(x => x.MedicalRecord)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedicalRecordHistory>()
            .HasOne(x => x.MedicalWorker)
            .WithMany()
            .HasForeignKey(x => x.MedicalWorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Account>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<MedicalWorkerAccount>()
            .HasIndex(account => account.MedicalWorkerId)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .ToTable("Accounts");

        modelBuilder.Entity<AdministratorAccount>()
            .ToTable("AdministratorAccounts");

        modelBuilder.Entity<MedicalWorkerAccount>()
            .ToTable("MedicalWorkerAccounts");

        modelBuilder.Entity<MedicalWorkerAccount>()
            .HasOne(account => account.MedicalWorker)
            .WithOne(worker => worker.Account)
            .HasForeignKey<MedicalWorkerAccount>(account => account.MedicalWorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.Account)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.AccountId);

        modelBuilder.Entity<MedicalWorker>()
            .Property(x => x.Role)
            .HasDefaultValue(MedicalWorkerRole.Other);

        modelBuilder.Entity<AuditEvent>()
            .HasKey(auditEvent => auditEvent.EventId);

        modelBuilder.Entity<AuditEvent>()
            .HasOne(auditEvent => auditEvent.Account)
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}