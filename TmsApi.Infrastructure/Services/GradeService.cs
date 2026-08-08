using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class GradeService(TmsDbContext context, ILogger<GradeService> logger) : IGradeService
{
    public async Task<GradeSubmissionResult> SubmitGradeAsync(
        int studentId, int courseId, decimal score, CancellationToken ct)
    {
        var enrollment = await context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId, ct);

        if (enrollment is null)
        {
            logger.LogWarning(
                "Grade submission failed: no enrollment for student {StudentId} in course {CourseId}",
                studentId, courseId);
            return new GradeSubmissionResult(0, false);
        }

        enrollment.Grade = score;
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Grade {Score} recorded for student {StudentId} in course {CourseId} (enrollment {EnrollmentId})",
            score, studentId, courseId, enrollment.Id);

        return new GradeSubmissionResult(enrollment.Id, true);
    }
}