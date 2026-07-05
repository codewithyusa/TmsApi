using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Services;

public class EnrollmentService(
    TmsDbContext context,
    ILogger<EnrollmentService> logger
) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);

    public async Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enrollment created: {EnrollmentId} for Course {CourseId}, Student {StudentId}",
            enrollment.Id,
            courseId,
            request.StudentId
        );

        var created = await GetByIdAsync(courseId, enrollment.Id, ct);
        return created!;
    }
}