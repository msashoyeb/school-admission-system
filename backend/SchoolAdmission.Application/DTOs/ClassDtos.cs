namespace SchoolAdmission.Application.DTOs;

public sealed record CreateClassRequest(string Name, int TotalSeats);

public sealed record ClassCreatedResponse(string Id, string Name, int TotalSeats);

public sealed record ClassSummaryResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);
