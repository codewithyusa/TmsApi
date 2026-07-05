using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Services;
using TmsApi.Dtos;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses/{courseId}/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    // GET by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        int courseId,
        int id,
        CancellationToken ct)
    {
        var record = await _enrollmentService.GetByIdAsync(courseId, id, ct);

        return record is not null ? Ok(record) : NotFound();
    }

    // POST create
    [HttpPost]
    public async Task<IActionResult> Create(
        int courseId,
        [FromBody] EnrollStudentRequest request,
        CancellationToken ct)
    {
        var record = await _enrollmentService.CreateAsync(courseId, request, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { courseId, id = record.Id },
            record
        );
    }
}