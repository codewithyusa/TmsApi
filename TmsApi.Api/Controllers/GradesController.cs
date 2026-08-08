using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/grades")]
[Tags("Grades")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class GradesController(IGradeService gradeService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(GradeSubmissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Submit a final grade for a student's enrollment")]
    public async Task<IActionResult> Submit([FromBody] SubmitGradeRequest request, CancellationToken ct)
    {
        var result = await gradeService.SubmitGradeAsync(
            request.StudentId, request.CourseId, request.Score, ct);

        return result.Success
            ? Ok(result)
            : NotFound(new ProblemDetails
            {
                Title = "Enrollment not found",
                Detail = $"No enrollment found for student {request.StudentId} in course {request.CourseId}.",
                Status = StatusCodes.Status404NotFound
            });
    }
}