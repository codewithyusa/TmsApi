namespace TmsApi.Domain.Entities;

public enum EnrollmentStatus
{
    Pending,
    Approved,
    Rejected
}

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public decimal? Grade { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}