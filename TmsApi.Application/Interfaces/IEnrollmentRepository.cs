using TmsApi.Entities;

namespace TmsApi.Application.Interfaces;

public interface IEnrollmentRepository
{
    Task<bool> ExistsAsync(
        int studentId,
        string courseCode,
        CancellationToken ct);

    Task AddAsync(
        Enrollment enrollment,
        CancellationToken ct);

    Task<List<Enrollment>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct);
}