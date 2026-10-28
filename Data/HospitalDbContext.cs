using HospitalManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Data;

public sealed class HospitalDbContext(DbContextOptions<HospitalDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Bill> Bills => Set<Bill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(x => x.DepartmentId);
            entity.Property(x => x.DepartmentName).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.DepartmentName).IsUnique();
            entity.HasData(
                new Department { DepartmentId = 1, DepartmentName = "Cardiology", Description = "Heart and circulatory care", IsActive = true },
                new Department { DepartmentId = 2, DepartmentName = "General Medicine", Description = "General outpatient services", IsActive = true },
                new Department { DepartmentId = 3, DepartmentName = "Orthopedics", Description = "Bone and joint services", IsActive = true },
                new Department { DepartmentId = 4, DepartmentName = "Pediatrics", Description = "Child health services", IsActive = true });
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(x => x.PatientId);
            entity.Property(x => x.PatientCode).HasMaxLength(24).IsRequired();
            entity.HasIndex(x => x.PatientCode).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(24).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Address).HasMaxLength(300).IsRequired();
            entity.Property(x => x.RegistrationDateUtc).HasColumnType("datetime(6)");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(x => x.DoctorId);
            entity.Property(x => x.DoctorCode).HasMaxLength(24).IsRequired();
            entity.HasIndex(x => x.DoctorCode).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Specialization).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.ConsultationFee).HasPrecision(10, 2);
            entity.HasOne(x => x.Department).WithMany(x => x.Doctors)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(x => x.AppointmentId);
            entity.Property(x => x.AppointmentCode).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.AppointmentCode).IsUnique();
            entity.Property(x => x.AppointmentDateTimeUtc).HasColumnType("datetime(6)");
            entity.Property(x => x.CreatedAtUtc).HasColumnType("datetime(6)");
            entity.Property(x => x.ReasonForVisit).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(x => new { x.DoctorId, x.AppointmentDateTimeUtc });
            entity.HasIndex(x => new { x.PatientId, x.AppointmentDateTimeUtc });
            entity.HasOne(x => x.Patient).WithMany(x => x.Appointments)
                .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Doctor).WithMany(x => x.Appointments)
                .HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(x => x.BillId);
            entity.Property(x => x.BillNumber).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.BillNumber).IsUnique();
            entity.HasIndex(x => x.AppointmentId).IsUnique();
            entity.Property(x => x.BillDateUtc).HasColumnType("datetime(6)");
            entity.Property(x => x.Subtotal).HasPrecision(10, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(10, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 4);
            entity.Property(x => x.TaxAmount).HasPrecision(10, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(10, 2);
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300).IsRequired();
            entity.HasOne(x => x.Patient).WithMany(x => x.Bills)
                .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Appointment).WithOne(x => x.Bill)
                .HasForeignKey<Bill>(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
