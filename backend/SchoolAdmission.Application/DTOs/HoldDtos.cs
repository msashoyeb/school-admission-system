using SchoolAdmission.Domain.Enums;
using System.ComponentModel.Design;

namespace SchoolAdmission.Application.DTOs;

public sealed record SeatHoldResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    HoldStatus Status,
    DateTimeOffset ExpiresAt);

public sealed record HoldDetailResponse(
    string HoldId,
    string ClassId,
    HoldStatus Status,
    DateTimeOffset ExpiresAt);
public sealed record ConfirmHoldResponse(
    string HoldId,
    HoldStatus Status);

public sealed record MyHoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    HoldStatus Status,
    DateTimeOffset ExpiresAt);
