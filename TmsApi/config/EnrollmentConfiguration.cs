using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations
{
    public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
    {
        public void Configure(EntityTypeBuilder<Enrollment> builder)
        {
            builder.HasKey(e => e.Id);

            // Foreign key relationships
            builder.HasOne(e => e.Student)
                   .WithMany(s => s.Enrollments)
                   .HasForeignKey(e => e.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Course)
                   .WithMany(c => c.Enrollments)
                   .HasForeignKey(e => e.CourseId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Grade is optional (nullable)
            builder.Property(e => e.Grade)
                   .HasPrecision(4, 2); // e.g. 95.50

            // Default enrollment timestamp
            builder.Property(e => e.EnrolledAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Prevent duplicate enrollments for the same student/course
            builder.HasIndex(e => new { e.StudentId, e.CourseId })
                   .IsUnique();

            // Archiving
            builder.Property(e => e.IsArchived)
                   .HasDefaultValue(false);
        }
    }
}
