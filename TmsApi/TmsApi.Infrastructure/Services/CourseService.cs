using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;
public class CourseService(
    TmsDbContext context,
    ILogger<CourseService> logger
) : ICourseService
{
    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);

    public async Task<CourseResponseDto> CreateAsync(
        CreateCourseRequest request,
        CancellationToken ct)
    {
        if (await CodeExistsAsync(request.Code, ct))
        {
            throw new InvalidOperationException(
                $"Course code '{request.Code}' already exists.");
        }

        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created course {CourseId} ({Code})",
            course.Id,
            course.Code);

        return (await GetByIdAsync(course.Id, ct))!;
    }

    // ✅ FIX: REQUIRED BY INTERFACE (THIS WAS MISSING)
    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
        PagedRequest request,
        CancellationToken ct)
    {
        var query = context.Courses.AsNoTracking();

        // SEARCH
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c =>
                c.Title.Contains(request.Search) ||
                c.Code.Contains(request.Search));
        }

        // COUNT BEFORE PAGING
        var totalCount = await query.CountAsync(ct);

        // SAFE ORDERING (whitelist)
        query = request.OrderBy.ToLower() switch
        {
            "code" => request.Descending
                ? query.OrderByDescending(c => c.Code)
                : query.OrderBy(c => c.Code),

            "title" => request.Descending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title),

            _ => request.Descending
                ? query.OrderByDescending(c => c.Id)
                : query.OrderBy(c => c.Id)
        };

        // PAGING
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .ToListAsync(ct);

        return new PagedResponse<CourseResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}