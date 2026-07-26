using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    TmsDbContext context,
    HybridCache cache,
    ILogger<CachedCourseService> logger
) : ICachedCourseService, ICourseService
{
    private const string SchemaVersion = "v2";
    private const string CourseTag = "courses";


    private static string AllCoursesCacheKey =>
        $"{SchemaVersion}:courses:all";


    private static string CourseByCodeCacheKey(string code) =>
        $"{SchemaVersion}:courses:code:{code}";



    // ============================
    // Hybrid Cache Methods
    // ============================


    public async Task<CourseDto> GetCourseAsync(
        string code,
        CancellationToken ct)
    {
        var key = CourseByCodeCacheKey(code);

        return await cache.GetOrCreateAsync(
            key,

            async cancel =>
            {
                logger.LogInformation(
                    "Cache MISS for {CacheKey} fetching from DB",
                    key);


                var course = await context.Courses
                    .AsNoTracking()
                    .Where(c => c.Code == code)
                    .Select(c => new CourseDto(
                        c.Id,
                        c.Title,
                        c.Code,
                        c.MaxCapacity,
                        c.Enrollments.Count))
                    .FirstOrDefaultAsync(cancel);


                if (course is null)
                {
                    throw new KeyNotFoundException(
                        $"Course '{code}' not found.");
                }


                return course;
            },

            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },

            tags:
            [
                CourseTag
            ],

            cancellationToken: ct);
    }



    public async Task<List<CourseDto>> GetAllCoursesAsync(
        CancellationToken ct)
    {
        var key = AllCoursesCacheKey;


        return await cache.GetOrCreateAsync(
            key,

            async cancel =>
            {
                logger.LogInformation(
                    "Cache MISS for {CacheKey} fetching from DB",
                    key);


                return await context.Courses
                    .AsNoTracking()
                    .Select(c => new CourseDto(
                        c.Id,
                        c.Title,
                        c.Code,
                        c.MaxCapacity,
                        c.Enrollments.Count))
                    .ToListAsync(cancel);
            },


            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },


            tags:
            [
                CourseTag
            ],

            cancellationToken: ct);
    }



    public async Task InvalidateCourseCacheAsync(
        CancellationToken ct)
    {
        await cache.RemoveByTagAsync(
            CourseTag,
            ct);


        logger.LogInformation(
            "Invalidating cache tag {Tag}",
            CourseTag);
    }





    // ============================
    // ICourseService Methods
    // ============================


    public Task<CourseResponseDto?> GetByIdAsync(
        int id,
        CancellationToken ct)
    {
        return context.Courses
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



    public Task<List<CourseResponseDto>> GetAllAsync(
        CancellationToken ct)
    {
        return context.Courses
            .AsNoTracking()
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .ToListAsync(ct);
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


        await InvalidateCourseCacheAsync(ct);


        logger.LogInformation(
            "Created course {CourseId}",
            course.Id);


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


        if (course is null)
            return null;


        course.Title = request.Title;


        await context.SaveChangesAsync(ct);



        await InvalidateCourseCacheAsync(ct);



        logger.LogInformation(
            "Updated course {CourseId}",
            id);



        return await GetByIdAsync(
            id,
            ct);
    }





    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
        PagedRequest request,
        CancellationToken ct)
    {
        var query = context.Courses
            .AsNoTracking();



        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c =>
                c.Title.Contains(request.Search) ||
                c.Code.Contains(request.Search));
        }



        var totalCount = await query.CountAsync(ct);



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