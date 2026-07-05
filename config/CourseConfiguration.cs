using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations
{
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                   .IsRequired()
                   .HasMaxLength(10);

            builder.Property(c => c.Title)
                   .IsRequired()
                   .HasMaxLength(200);

           builder.HasIndex(c => c.Code).IsUnique();

            builder.Property(c => c.MaxCapacity)
                   .HasDefaultValue(30);

            // Relationships
            builder.HasMany(c => c.Enrollments)
                   .WithOne(e => e.Course)
                   .HasForeignKey(e => e.CourseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Assessments)
                   .WithOne(a => a.Course)
                   .HasForeignKey(a => a.CourseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Certificates)
                   .WithOne(cert => cert.Course)
                   .HasForeignKey(cert => cert.CourseId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
