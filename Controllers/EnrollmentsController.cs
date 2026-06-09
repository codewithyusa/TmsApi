using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    // GET: api/enrollments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await _enrollmentService.GetAllAsync();
        return Ok(enrollments);
    }

    // GET: api/enrollments/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await _enrollmentService.GetByIdAsync(id);

        if (record is null)
            return NotFound();

        return Ok(record);
    }

    // POST: api/enrollments
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await _enrollmentService.EnrollAsync(
            request.StudentId,
            request.CourseCode
        );

        return CreatedAtAction(
            nameof(GetById),
            new { id = record.Id },
            record
        );
    }
}

// Request DTO
public record CreateEnrollmentRequest(string StudentId, string CourseCode);