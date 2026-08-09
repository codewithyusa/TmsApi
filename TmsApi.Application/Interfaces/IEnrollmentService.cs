using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Application.Dtos;
namespace TmsApi.Application.Interfaces;
public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct);
    Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct);
    Task<List<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct);

    // New: approve an enrollment by its own Id (not scoped to a course)
    Task<bool> ApproveAsync(int id, CancellationToken ct);
Task<List<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct);
}