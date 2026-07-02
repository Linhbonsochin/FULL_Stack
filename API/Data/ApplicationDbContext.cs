using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<AttendanceLog> AttendanceLogs { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<AttendanceSummary> AttendanceSummaries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Suppress PendingModelChangesWarning
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee configuration
        modelBuilder.Entity<Employee>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<Employee>()
            .Property(e => e.EmployeeCode)
            .HasMaxLength(50)
            .IsRequired();
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeCode)
            .IsUnique();
        modelBuilder.Entity<Employee>()
            .Property(e => e.FullName)
            .HasMaxLength(255)
            .IsRequired();
        modelBuilder.Entity<Employee>()
            .Property(e => e.Status)
            .HasMaxLength(20);

        // Shift configuration
        modelBuilder.Entity<Shift>()
            .HasKey(s => s.Id);
        modelBuilder.Entity<Shift>()
            .Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();
        modelBuilder.Entity<Shift>()
            .Property(s => s.Status)
            .HasMaxLength(20);

        // AttendanceLog configuration
        modelBuilder.Entity<AttendanceLog>()
            .HasKey(a => a.Id);

        // Employee FK
        modelBuilder.Entity<AttendanceLog>()
            .HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Shift FK
        modelBuilder.Entity<AttendanceLog>()
            .HasOne(a => a.Shift)
            .WithMany(s => s.AttendanceLogs)
            .HasForeignKey(a => a.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Status enum conversion (C# enum to int in database)
        modelBuilder.Entity<AttendanceLog>()
            .Property(a => a.Status)
            .HasConversion(
                v => (int)v,                    // Enum to int for storage
                v => (AttendanceStatus)v        // int to Enum when read
            )
            .HasColumnType("int");

        // OvertimeHours column type
        modelBuilder.Entity<AttendanceLog>()
            .Property(a => a.OvertimeHours)
            .HasColumnType("decimal(5,2)");

        // StandardWorkday column type
        modelBuilder.Entity<AttendanceLog>()
            .Property(a => a.StandardWorkday)
            .HasColumnType("decimal(5,1)");

        // Indexes
        modelBuilder.Entity<AttendanceLog>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            ; // non-unique: allow multiple sessions per employee per day
        modelBuilder.Entity<AttendanceLog>()
            .HasIndex(a => a.Date);

        // LeaveRequest configuration
        modelBuilder.Entity<LeaveRequest>()
            .HasKey(l => l.Id);
        modelBuilder.Entity<LeaveRequest>()
            .Property(l => l.Status)
            .HasMaxLength(20);
        modelBuilder.Entity<LeaveRequest>()
            .Property(l => l.Reason)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<LeaveRequest>()
            .HasIndex(l => l.Status);

        // AttendanceSummary configuration
        modelBuilder.Entity<AttendanceSummary>()
            .HasKey(s => s.Id);
        modelBuilder.Entity<AttendanceSummary>()
            .HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttendanceSummary>()
            .HasIndex(s => new { s.EmployeeId, s.Year, s.Month })
            .IsUnique();
        modelBuilder.Entity<AttendanceSummary>()
            .Property(s => s.TotalStandardWorkdays)
            .HasColumnType("decimal(5,1)");
        modelBuilder.Entity<AttendanceSummary>()
            .Property(s => s.TotalAnnualLeaveDays)
            .HasColumnType("decimal(5,1)");
        modelBuilder.Entity<AttendanceSummary>()
            .Property(s => s.TotalSickLeaveDays)
            .HasColumnType("decimal(5,1)");
        modelBuilder.Entity<AttendanceSummary>()
            .Property(s => s.TotalUnpaidLeaveDays)
            .HasColumnType("decimal(5,1)");
        modelBuilder.Entity<AttendanceSummary>()
            .Property(s => s.TotalAbsentDays)
            .HasColumnType("decimal(5,1)");
        modelBuilder.Entity<AttendanceSummary>()
            .Property(s => s.TotalOvertimeHours)
            .HasColumnType("decimal(5,2)");

        // Seed default shifts
        modelBuilder.Entity<Shift>().HasData(
            new Shift
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Ca Hành chính",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 30, 0),
                GracePeriodMinutes = 15,
                Status = "Active",
                Description = "Ca làm việc chính từ 8:00 đến 17:30"
            },
            new Shift
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "Ca Sáng",
                StartTime = new TimeSpan(7, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                GracePeriodMinutes = 15,
                Status = "Active",
                Description = "Ca sáng từ 7:00 đến 12:00"
            },
            new Shift
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "Ca Chiều",
                StartTime = new TimeSpan(13, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                GracePeriodMinutes = 15,
                Status = "Active",
                Description = "Ca chiều từ 13:00 đến 18:00"
            }
        );
    }
}
