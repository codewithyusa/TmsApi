namespace TmsApi.Application.Dtos;

public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount
);


public static class CourseResponseDtoFields
{
    public static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(CourseResponseDto.Id),
            nameof(CourseResponseDto.Code),
            nameof(CourseResponseDto.Title),
            nameof(CourseResponseDto.MaxCapacity),
            nameof(CourseResponseDto.EnrollmentCount)
        };
}