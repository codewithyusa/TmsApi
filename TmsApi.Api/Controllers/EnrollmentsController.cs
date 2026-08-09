using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Api.Hubs;
using TmsApi.Application.Dtos;
using TmsApi.Application.Hubs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
[Tags("Enrollments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService,
    IHubContext<TmsHub, ITmsHubClient> hubContext,
    ILogger<EnrollmentsController> logger
) : ControllerBase
{
    [HttpGet(Name = "ListCourseEnrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List enrolments for a course")]
    public async Task<IActionResult> GetEnrollments(
        int courseId,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);

        if (course is null)
            return NotFound();

        var enrollments = await enrollmentService.GetByCourseAsync(courseId, ct);

        return Ok(enrollments);
    }

    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one enrolment for a course")]
    public async Task<IActionResult> GetEnrollment(
        int courseId,
        int id,
        CancellationToken ct)
    {
        var enrollment = await enrollmentService.GetByIdAsync(courseId, id, ct);

        return enrollment is not null
            ? Ok(enrollment)
            : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Enrol a student in a course")]
    [EndpointDescription("Returns 404 if the course does not exist, 409 if the course has reached MaxCapacity.")]
    public async Task<IActionResult> EnrollStudent(
        int courseId,
        [FromBody] EnrollStudentRequest request,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);

        if (course is null)
            return NotFound();

        if (course.EnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var enrollment = await enrollmentService.CreateAsync(courseId, request, ct);

        return CreatedAtAction(
            nameof(GetEnrollment),
            new { courseId, id = enrollment.Id },
            enrollment
        );
    }

    [HttpGet("~/api/enrollments", Name = "ListAllEnrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List all enrollments across all courses")]
    public async Task<IActionResult> GetAllEnrollments(CancellationToken ct)
    {
        var enrollments = await enrollmentService.GetAllAsync(ct);
        return Ok(enrollments);
    }

    [HttpPost("~/api/enrollments/{id:int}/approve", Name = "ApproveEnrollment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Approve a pending enrollment")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var approved = await enrollmentService.ApproveAsync(id, ct);
        if (!approved)
            return NotFound();

        try
        {
            await hubContext.Clients.All
                .ReceiveEnrollmentStatusUpdated(id.ToString(), "Approved");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast enrollment {Id} approval", id);
        }

        return NoContent();
    }
}