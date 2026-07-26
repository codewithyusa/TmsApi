using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    ICourseService service,
    ILogger<CachedCourseService> logger)
    : ICachedCourseService
{
    public async Task<CourseDto> GetCourseAsync(
        string code,
        CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            (service, code),
            async (state, token) =>
            {
                dbHit = true;

                logger.LogInformation(
                    "Cache MISS for {Key} fetching from DB",
                    key);

                var course = await state.service.GetByCodeAsync(
                    state.code,
                    token);

                if (course == null)
                {
                    throw new NotFoundException(
                        $"Course {state.code} not found.");
                }

                return new CourseDto(
                    course.Id,
                    course.Title,
                    course.Code,
                    course.MaxCapacity,
                    course.Enrollments.Count);
            },
            options: new HybridCacheEntryOptions(),
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation(
                "Cache HIT for {Key}",
                key);
        }

        return dto;
    }


    public async Task<List<CourseDto>> GetAllCoursesAsync(
        CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var list = await cache.GetOrCreateAsync(
            key,
            service,
            async (state, token) =>
            {
                dbHit = true;

                logger.LogInformation(
                    "Cache MISS for {Key} fetching from DB",
                    key);

                var courses = await state.GetAllAsync(token);

                return courses.Select(c => new CourseDto(
                    c.Id,
                    c.Title,
                    c.Code,
                    c.MaxCapacity,
                    c.Enrollments.Count))
                    .ToList();
            },
            options: new HybridCacheEntryOptions(),
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation(
                "Cache HIT for {Key}",
                key);
        }

        return list;
    }


    public async Task InvalidateCourseCacheAsync(
        CancellationToken ct)
    {
        logger.LogInformation(
            "Invalidating cache tag {Tag}",
            CacheKeys.CoursesTag);

        await cache.RemoveByTagAsync(
            CacheKeys.CoursesTag,
            ct);
    }
}