using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.HasKey(s => s.Id);

            // Natural key: RegistrationNumber must be unique
            builder.HasIndex(s => s.RegistrationNumber).IsUnique();

            builder.Property(s => s.RegistrationNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(s => s.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(s => s.GPA)
                   .HasPrecision(3, 2); // e.g. 3.75

            builder.Property(s => s.IsActive)
                   .HasDefaultValue(true);

            // Relationships
            builder.HasMany(s => s.Enrollments)
                   .WithOne(e => e.Student)
                   .HasForeignKey(e => e.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Certificates)
                   .WithOne(c => c.Student)
                   .HasForeignKey(c => c.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(s => s.RowVersion)
                    .IsRowVersion();

           builder.Property(s => s.IsDeleted)
                  .HasDefaultValue(false);

           builder.HasQueryFilter(s => !s.IsDeleted);

           builder.Property<DateTime>("LastUpdated");
      
        }
    }
}
