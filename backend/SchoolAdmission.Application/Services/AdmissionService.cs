using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services
{
    public sealed class AdmissionService(IAdmissionRepository repository, AdmissionSeatHoldingTime holdingTime) : IAdmissionService
    {
        public Task<ClassCreatedResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null)
                throw new ValidationException("Request body is required.");

            var trimmedName = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new ValidationException("Class name is required.");


            if (trimmedName.Length > 200)
                throw new ValidationException("Class name must be 200 characters or fewer.");

            if (request.TotalSeats <= 0)
                throw new ValidationException("Total seats must be greater than zero.");

            return repository.CreateClassAsync(trimmedName, request.TotalSeats, cancellationToken);
        }

        public Task<IReadOnlyList<ClassSummaryResponse>> GetClassesAsync(CancellationToken cancellationToken = default)
        {
            return repository.GetClassesAsync(cancellationToken);
        }

        public async Task<ClassSummaryResponse> GetClassByIdAsync(string classId, CancellationToken cancellationToken = default)
        {
            var normalizedClassId = ValidateIdentifier(classId, "Class ID");
            return await repository.GetClassByIdAsync(normalizedClassId, cancellationToken)
                ?? throw new NotFoundException($"Class '{normalizedClassId}' was not found.");
        }

        public Task<SeatHoldResponse> CreateHoldAsync(string classId, string? studentId,
            CancellationToken cancellationToken = default)
        {
            var normalizedClassId = ValidateIdentifier(classId, "Class ID");
            var normalizedStudentId = ValidateStudentId(studentId);
            var durationSeconds = holdingTime.HoldDurationSeconds > 0 ? holdingTime.HoldDurationSeconds : 600;

            return repository.CreateHoldAsync(
                normalizedClassId,
                normalizedStudentId,
                TimeSpan.FromSeconds(durationSeconds),
                cancellationToken);
        }

        public async Task<HoldDetailResponse> GetHoldByIdAsync(string holdId, CancellationToken cancellationToken = default)
        {
            var normalizedHoldId = ValidateIdentifier(holdId, "Hold ID");
            return await repository.GetHoldByIdAsync(normalizedHoldId, cancellationToken)
                ?? throw new NotFoundException($"Application hold '{normalizedHoldId}' was not found.");
        }

        public Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string? studentId,
            CancellationToken cancellationToken = default)
        {
            var normalizedHoldId = ValidateIdentifier(holdId, "Hold ID");
            var normalizedStudentId = ValidateStudentId(studentId);
            return repository.ConfirmHoldAsync(normalizedHoldId, normalizedStudentId, cancellationToken);
        }

        public Task CancelHoldAsync(
            string holdId,
            string? studentId,
            CancellationToken cancellationToken = default)
        {
            var normalizedHoldId = ValidateIdentifier(holdId, "Hold ID");
            var normalizedStudentId = ValidateStudentId(studentId);
            return repository.CancelHoldAsync(normalizedHoldId, normalizedStudentId, cancellationToken);
        }

        public Task<IReadOnlyList<MyHoldResponse>> GetStudentHoldsAsync(
            string? studentId,
            CancellationToken cancellationToken = default)
        {
            var normalizedStudentId = ValidateStudentId(studentId);
            return repository.GetStudentHoldsAsync(normalizedStudentId, cancellationToken);
        }

        private static string ValidateIdentifier(string value, string fieldName)
        {
            var trimmed = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException($"{fieldName} is required.");

            if (trimmed.Length > 64)
                throw new ValidationException($"{fieldName} must be 64 characters or fewer.");

            return trimmed;
        }

        private static string ValidateStudentId(string? studentId)
        {
            var trimmed = studentId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new ValidationException("X-Student-Id header is required.");
            }

            if (trimmed.Length > 64)
            {
                throw new ValidationException("X-Student-Id must be 64 characters or fewer.");
            }

            return trimmed;
        }
    }
}
