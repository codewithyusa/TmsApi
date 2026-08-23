namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    // Surrogate primary key — internal, used by foreign keys.

    public required string Code { get; set; }
    // Natural key — human-readable.

    public required string Title { get; set; }

    public int MaxCapacity { get; set; }

    // Identity user ID of the instructor assigned to this course.
    // Used by resource-based authorization.
    public string? InstructorId { get; set; }

    // Navigation property for many-to-many relationship.
    public ICollection<Enrollment> Enrollments { get; set; }
        = new List<Enrollment>();
}
