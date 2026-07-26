namespace TmsApi.Application.Interfaces;

using TmsApi.Application.DTOs;

public interface ICachedCourseService : ICourseService
{
    Task<CourseDto> GetCourseAsync(string code, CancellationToken ct);
    Task<List<CourseDto>> GetAllCoursesAsync(CancellationToken ct);
    Task InvalidateCourseCacheAsync(CancellationToken ct);
}
