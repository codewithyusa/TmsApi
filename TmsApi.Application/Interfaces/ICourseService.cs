namespace TmsApi.Application.Interfaces;

using TmsApi.Application.Dtos;

public interface ICourseService
{
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<CourseResponseDto?> GetByCodeAsync(
        string code,
        CancellationToken ct);

    Task<List<CourseResponseDto>> GetAllAsync(
        CancellationToken ct);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken ct);

    Task<CourseResponseDto> CreateAsync(
        CreateCourseRequest request,
        CancellationToken ct);

    Task<CourseResponseDto?> UpdateAsync(
        int id,
        UpdateCourseRequest request,
        CancellationToken ct);

    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
        PagedRequest request,
        CancellationToken ct);
}