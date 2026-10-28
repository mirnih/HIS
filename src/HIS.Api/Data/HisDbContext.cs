using HIS.Api.Models;
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
    }
}