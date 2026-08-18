using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public class TmsDbContext : IdentityDbContext<TmsUser>
{
    public TmsDbContext(DbContextOptions<TmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<Certificate> Certificates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Important: this creates the ASP.NET Core Identity tables/configuration.
        base.OnModelCreating(modelBuilder);

        // Enrollment -> Student
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enrollment -> Course
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Shadow audit property
        modelBuilder.Entity<Student>()
            .Property<DateTime>("LastUpdated");

        // Concurrency token
        modelBuilder.Entity<Student>()
            .Property(s => s.Version)
            .IsRowVersion();

        // Soft-delete filter
        modelBuilder.Entity<Student>()
            .HasQueryFilter(s => !s.IsDeleted);
    }

    public override int SaveChanges()
    {
        UpdateAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        UpdateAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAudit()
    {
        foreach (var entry in ChangeTracker.Entries<Student>())
        {
            if (entry.State == EntityState.Added ||
                entry.State == EntityState.Modified)
            {
                entry.Property("LastUpdated").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}