namespace TmsApi.Domain.Entities;

public class Assessment
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public decimal MaxScore { get; set; }

    public decimal Weight { get; set; }
    // share of final grade (e.g. 0.30m = 30%)

    // Foreign key + navigation to Course
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}