namespace TmsApi.Domain.Entities;

public class Student
{
    public int Id { get; set; }

    // surrogate primary key
    public required string RegistrationNumber { get; set; }

    // natural key (business identifier)
    public required string Name { get; set; }

    public decimal GPA { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;

    // Keep this if your previous exercises still use it
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    // Concurrency token
    public uint Version { get; set; }
}