using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly TmsDbContext _context;

    public ReportController(TmsDbContext context)
    {
        _context = context;
    }

    // Exercise 3 - Task 1: Pagination
    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 20;

        var students = await _context.Students
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    // Exercise 3 - Task 2: Top 5 courses by enrollment
    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses(
        CancellationToken cancellationToken)
    {
        var courses = await _context.Enrollments
            .GroupBy(e => new
            {
                e.CourseId,
                e.Course.Title
            })
            .Select(g => new
            {
                CourseTitle = g.Key.Title,
                EnrollmentCount = g.Count()
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }
}