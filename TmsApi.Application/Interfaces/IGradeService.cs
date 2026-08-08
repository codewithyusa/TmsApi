using TmsApi.Application.Dtos;

namespace TmsApi.Application.Interfaces;

public interface IGradeService
{
    Task<GradeSubmissionResult> SubmitGradeAsync(int studentId, int courseId, decimal score, CancellationToken ct);
}