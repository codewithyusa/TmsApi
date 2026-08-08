namespace TmsApi.Application.Dtos;

public record SubmitGradeRequest(int StudentId, int CourseId, decimal Score);

public record GradeSubmissionResult(int Id, bool Success);