using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

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
                e.Course.Title,
                e.StudentId,
                e.Student.Name,
                e.Status.ToString(),
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);

    public async Task<List<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.Course.Title,
                e.StudentId,
                e.Student.Name,
                e.Status.ToString(),
                e.EnrolledAt))
            .ToListAsync(ct);
    }

    public async Task<List<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.Course.Title,
                e.StudentId,
                e.Student.Name,
                e.Status.ToString(),
                e.EnrolledAt))
            .ToListAsync(ct);
    }

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

    public async Task<bool> ApproveAsync(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (enrollment is null)
        {
            logger.LogWarning("ApproveAsync: enrollment {EnrollmentId} not found", id);
            return false;
        }

        enrollment.Status = EnrollmentStatus.Approved;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment {EnrollmentId} approved", id);
        return true;
    }
}