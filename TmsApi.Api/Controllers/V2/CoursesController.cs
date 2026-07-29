using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Utilities;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
[Tags("Courses")]
[Produces("application/json")]
public class CoursesController(
    ICourseService courseService,
    ICachedCourseService cachedCourseService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var courses = await courseService.GetCoursesAsync(
            request,
            ct);

        var shaped = courses.Items.ShapeData(
            fields,
            CourseResponseDtoFields.Allowed);

        var links = new List<LinkDto>
        {
            new(
                Url.Action(
                    nameof(GetCourses),
                    new
                    {
                        page = courses.Page,
                        fields
                    })!,
                "self",
                "GET")
        };

        if (courses.HasNext)
        {
            links.Add(
                new LinkDto(
                    Url.Action(
                        nameof(GetCourses),
                        new
                        {
                            page = courses.Page + 1,
                            fields
                        })!,
                    "next",
                    "GET"));
        }

        if (courses.HasPrevious)
        {
            links.Add(
                new LinkDto(
                    Url.Action(
                        nameof(GetCourses),
                        new
                        {
                            page = courses.Page - 1,
                            fields
                        })!,
                    "prev",
                    "GET"));
        }

        return Ok(new
        {
            Data = shaped,
            Meta = new
            {
                courses.TotalCount,
                courses.Page,
                courses.TotalPages,
                courses.HasNext,
                courses.HasPrevious
            },
            Links = links
        });
    }


    // Ex3/Ex9 test endpoint: goes through HybridCache + tms.cache.hits/misses metering.
    // Call this repeatedly to see one "Cache MISS" log then subsequent hits with no DB log line.
    [HttpGet("cached")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCachedCourses(CancellationToken ct)
    {
        var courses = await cachedCourseService.GetAllCoursesAsync(ct);
        return Ok(courses);
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourse(
        int id,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(
            id,
            ct);

        if (course is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            Data = course,
            Links = new[]
            {
                new LinkDto(
                    Url.Action(
                        nameof(GetCourse),
                        new { id })!,
                    "self",
                    "GET"),

                new LinkDto(
                    Url.Action(
                        "Enroll",
                        "Enrollments",
                        new { courseId = id })!,
                    "enroll",
                    "POST")
            }
        });
    }
}