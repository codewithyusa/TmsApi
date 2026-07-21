using System;

namespace TmsApi.Domain.Entities;

public class Certificate
{
    // surrogate primary key
    public int Id { get; set; }

    public required string SerialNumber { get; set; }
    // natural key — human-readable (uniqueness configured later)

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    public int StudentId { get; set; }
    public int CourseId { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}