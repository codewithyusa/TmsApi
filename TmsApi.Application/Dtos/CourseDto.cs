namespace TmsApi.Application.DTOs;

public record CourseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount);


public static class CourseDtoFields
{
    public static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(CourseDto.Id),
            nameof(CourseDto.Code),
            nameof(CourseDto.Title),
            nameof(CourseDto.MaxCapacity),
            nameof(CourseDto.EnrollmentCount)
        };
}