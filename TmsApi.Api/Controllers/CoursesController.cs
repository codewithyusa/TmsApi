using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[Authorize(Roles = "Instructor,Admin")]
[ApiController]
[Route("api/courses")]
[Tags("Courses")]
[Produces("application/json")]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
public class CoursesController(
    ICourseService courseService,
    LinkGenerator linkGenerator,
    TmsDbContext context,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    [ProducesResponseType(
        typeof(CourseDetailDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID")]
    [EndpointDescription(
        "Returns course details with HATEOAS links. Returns 404 if the course does not exist.")]
    public async Task<IActionResult> GetCourseById(
        int id,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);

        if (course is null)
            return NotFound();

        var courseUrl = linkGenerator.GetPathByName(
            HttpContext,
            nameof(GetCourseById),
            new { id });

        if (courseUrl is null)
        {
            throw new InvalidOperationException(
                "Course route could not be generated.");
        }

        var enrollmentsUrl = linkGenerator.GetPathByAction(
            HttpContext,
            action: "GetEnrollments",
            controller: "Enrollments",
            values: new { courseId = id });

        if (enrollmentsUrl is null)
        {
            throw new InvalidOperationException(
                "Enrollment route could not be generated.");
        }

        var links = new List<LinkDto>
        {
            new(courseUrl, "self", "GET"),
            new(courseUrl, "update", "PUT"),
            new(courseUrl, "delete", "DELETE"),
            new(enrollmentsUrl, "enrollments", "GET")
        };

        if (course.EnrollmentCount < course.MaxCapacity)
        {
            links.Add(new LinkDto(
                enrollmentsUrl,
                "enroll",
                "POST"));
        }

        var detailDto = new CourseDetailDto
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            MaxCapacity = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount,
            Links = links
        };

        return Ok(detailDto);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<CourseResponseDto>),
        StatusCodes.Status200OK)]
    [EndpointSummary("List courses with pagination")]
    [EndpointDescription(
        "Returns a paginated, optionally filtered list of TMS courses. PageSize is capped at 50.")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result =
            await courseService.GetCoursesAsync(request, ct);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CourseResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new course")]
    [EndpointDescription(
        "Creates a course with a unique code. Returns 409 if the course code already exists.")]
    public async Task<IActionResult> CreateCourse(
        CreateCourseRequest request,
        CancellationToken ct)
    {
        if (await courseService.CodeExistsAsync(
                request.Code,
                ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course code already exists",
                Detail =
                    $"A course with code '{request.Code}' is already registered.",
                Status =
                    StatusCodes.Status409Conflict
            });
        }

        var result =
            await courseService.CreateAsync(
                request,
                ct);

        return CreatedAtAction(
            nameof(GetCourseById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(
        typeof(CourseResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [EndpointSummary("Update a course")]
    [EndpointDescription(
        "Updates course information after resource-based authorization.")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        UpdateCourseRequest request,
        CancellationToken ct)
    {
        // Load the actual Course entity.
        //
        // The authorization handler needs the Course resource
        // so it can compare InstructorId with the logged-in user.
        var course = await context.Courses
            .FirstOrDefaultAsync(
                c => c.Id == id,
                ct);

        if (course is null)
        {
            return NotFound();
        }

        // Resource-based authorization.
        var authResult =
            await authorizationService.AuthorizeAsync(
                User,
                course,
                "CanEditCourse");

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        // User is authorized to update this course.
        var result =
            await courseService.UpdateAsync(
                id,
                request,
                ct);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
