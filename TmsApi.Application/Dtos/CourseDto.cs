namespace TmsApi.Application.DTOs;

public record CourseDto(
    int Id,
    string Title,
    string Code,
    int MaxCapacity,
    int EnrollmentCount);