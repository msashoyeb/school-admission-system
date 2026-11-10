namespace SchoolAdmission.Domain.Exceptions
{
    public abstract class AppException(string message, int statusCode) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
    }

    public sealed class ValidationException(string message) : AppException(message, 400);
    public sealed class ForbiddenException(string message) : AppException(message, 403);
    public sealed class NotFoundException(string message) : AppException(message, 404);
    public sealed class ConflictException(string message) : AppException(message, 409);
}
