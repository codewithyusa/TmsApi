using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using System.Linq;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly TmsDbContext _context;

    public TestController(TmsDbContext context)
    {
        _context = context;
    }
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");

        try
        {
            var students = _context.Students
                .Where(s => IsHonorRoll(s.GPA)) // ❌ cannot be translated to SQL
                .ToList();

            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");

            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }

    [HttpGet("n1-demo")]
    public async Task<IActionResult> N1Demo(CancellationToken cancellationToken)
    {
        var students = await _context.Students
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var s in students)
        {
            var count = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, cancellationToken);

            Console.WriteLine($"{s.Name}: {count} enrollments");
        }

        return Ok("Check console logs for N+1 queries");
    }
}