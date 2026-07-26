using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Caching;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class CourseService(
    TmsDbContext context,
    ILogger<CourseService> logger,
    HybridCache cache
) : ICourseService
{

    public async Task<List<CourseResponseDto>> GetAllAsync(
        CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.AllCourses,

            async token =>
            {
                logger.LogInformation(
                    "Cache MISS for {Key} fetching from DB",
                    CacheKeys.AllCourses);


                return await context.Courses
                    .AsNoTracking()
                    .Select(c => new CourseResponseDto(
                        c.Id,
                        c.Code,
                        c.Title,
                        c.MaxCapacity,
                        c.Enrollments.Count))
                    .ToListAsync(token);

            },

            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            },

            tags: ["courses"],

            cancellationToken: ct);
    }


    public async Task<CourseResponseDto?> GetByIdAsync(
        int id,
        CancellationToken ct)
    {
        return await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
    }


    public Task<CourseResponseDto?> GetByCodeAsync(
        string code,
        CancellationToken ct)
    {
        return context.Courses
            .AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
    }


    public Task<bool> CodeExistsAsync(
        string code,
        CancellationToken ct)
    {
        return context.Courses
            .AnyAsync(c => c.Code == code, ct);
    }


    public async Task<CourseResponseDto> CreateAsync(
        CreateCourseRequest request,
        CancellationToken ct)
    {

        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };


        context.Courses.Add(course);

        await context.SaveChangesAsync(ct);


        await cache.RemoveByTagAsync(
            "courses",
            ct);


        logger.LogInformation(
            "Invalidating cache tag courses");


        return (await GetByIdAsync(course.Id, ct))!;
    }



    public async Task<CourseResponseDto?> UpdateAsync(
        int id,
        UpdateCourseRequest request,
        CancellationToken ct)
    {

        var course = await context.Courses
            .FirstOrDefaultAsync(
                c => c.Id == id,
                ct);


        if(course is null)
            return null;


        course.Title = request.Title;


        await context.SaveChangesAsync(ct);


        await cache.RemoveByTagAsync(
            "courses",
            ct);


        logger.LogInformation(
            "Invalidating cache tag courses");


        return await GetByIdAsync(id, ct);
    }



    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
        PagedRequest request,
        CancellationToken ct)
    {
        var query = context.Courses.AsNoTracking();


        var totalCount = await query.CountAsync(ct);


        var items = await query
            .OrderBy(c => c.Id)
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