using SchoolAdmission.Application.DTOs;

namespace SchoolAdmission.Application.Interfaces;

public interface IAdmissionRepository
{
    Task<ClassCreatedResponse> CreateClassAsync(string name, int totalSeats, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassSummaryResponse>> GetClassesAsync(CancellationToken cancellationToken = default);
    Task<ClassSummaryResponse?> GetClassByIdAsync(string classId, CancellationToken cancellationToken = default);
    Task<SeatHoldResponse> CreateHoldAsync(string classId, string studentId, TimeSpan holdDuration, CancellationToken cancellationToken = default);
    Task<HoldDetailResponse?> GetHoldByIdAsync(string holdId, CancellationToken cancellationToken = default);
    Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string studentId, CancellationToken cancellationToken = default);
    Task CancelHoldAsync(string holdId, string studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyHoldResponse>> GetStudentHoldsAsync(string studentId, CancellationToken cancellationToken = default);
    Task<int> ExpireOverdueHoldsAsync(CancellationToken cancellationToken = default);
}
