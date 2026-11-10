using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Repositories;

public sealed class AdmissionRepository(AdmissionDbContext dbContext) : IAdmissionRepository
{
    public async Task<ClassCreatedResponse> CreateClassAsync(
        string name,
        int totalSeats,
        CancellationToken cancellationToken = default)
    {
        var entity = new AdmissionClass
        {
            Id = NewId("c"),
            Name = name,
            TotalSeats = totalSeats,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Classes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ClassCreatedResponse(entity.Id, entity.Name, entity.TotalSeats);
    }

    public async Task<IReadOnlyList<ClassSummaryResponse>> GetClassesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var items = await dbContext.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
            })
            .ToListAsync(cancellationToken);

        return items
            .Select(x => new ClassSummaryResponse(
                x.Id,
                x.Name,
                x.TotalSeats,
                x.HeldSeats,
                x.ConfirmedSeats,
                Math.Max(0, x.TotalSeats - x.HeldSeats - x.ConfirmedSeats)))
            .ToList();
    }

    public async Task<ClassSummaryResponse?> GetClassByIdAsync(
        string classId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var item = await dbContext.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
            return null;

        return new ClassSummaryResponse(
            item.Id,
            item.Name,
            item.TotalSeats,
            item.HeldSeats,
            item.ConfirmedSeats,
            Math.Max(0, item.TotalSeats - item.HeldSeats - item.ConfirmedSeats));
    }

    public async Task<SeatHoldResponse> CreateHoldAsync(
        string classId,
        string studentId,
        TimeSpan holdDuration,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var classes = await dbContext.Classes
            .FromSqlInterpolated($"SELECT id, name, total_seats, created_at FROM classes WHERE id = {classId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var admissionClass = classes.SingleOrDefault()
            ?? throw new NotFoundException($"Class '{classId}' was not found.");

        var now = DateTimeOffset.UtcNow;

        await dbContext.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(h => h.Status, HoldStatus.EXPIRED)
                    .SetProperty(h => h.ExpiredAt, now),
                cancellationToken);

        var hasActiveOrConfirmedHold = await dbContext.Holds.AnyAsync(
            h => h.ClassId == classId &&
                 h.StudentId == studentId &&
                 (h.Status == HoldStatus.HELD || h.Status == HoldStatus.CONFIRMED),
            cancellationToken);

        if (hasActiveOrConfirmedHold)
        {
            throw new ConflictException("Student already has an active hold or confirmed admission for this class.");
        }

        var occupiedSeats = await dbContext.Holds.CountAsync(
            h => h.ClassId == classId &&
                 (h.Status == HoldStatus.HELD || h.Status == HoldStatus.CONFIRMED),
            cancellationToken);

        if (occupiedSeats >= admissionClass.TotalSeats)
        {
            throw new ConflictException("No seats available for this class.");
        }

        var hold = new SeatHold
        {
            Id = NewId("h"),
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.HELD,
            CreatedAt = now,
            ExpiresAt = now.Add(holdDuration)
        };

        dbContext.Holds.Add(hold);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException("Student already has an active hold or confirmed admission for this class.");
        }

        return new SeatHoldResponse(hold.Id, hold.ClassId, hold.StudentId, hold.Status, hold.ExpiresAt);
    }

    public async Task<HoldDetailResponse?> GetHoldByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default)
    {
        var hold = await dbContext.Holds
            .AsNoTracking()
            .SingleOrDefaultAsync(h => h.Id == holdId, cancellationToken);

        if (hold is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var status = hold.Status;

        if (status == HoldStatus.HELD && hold.ExpiresAt <= now)
        {
            status = HoldStatus.EXPIRED;
            await dbContext.Holds
                .Where(h => h.Id == holdId && h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(h => h.Status, HoldStatus.EXPIRED)
                        .SetProperty(h => h.ExpiredAt, now),
                    cancellationToken);
        }

        return new HoldDetailResponse(hold.Id, hold.ClassId, status, hold.ExpiresAt);
    }

    public async Task<ConfirmHoldResponse> ConfirmHoldAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var holds = await dbContext.Holds
            .FromSqlInterpolated(
                $"SELECT id, class_id, student_id, status, created_at, expires_at, confirmed_at, cancelled_at, expired_at FROM holds WHERE id = {holdId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        var hold = holds.SingleOrDefault()
            ?? throw new NotFoundException($"Application hold '{holdId}' was not found.");

        if (!string.Equals(hold.StudentId, studentId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You can only confirm your own application.");
        }

        if (hold.Status == HoldStatus.CONFIRMED)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ConfirmHoldResponse(hold.Id, HoldStatus.CONFIRMED);
        }

        if (hold.Status == HoldStatus.CANCELLED)
        {
            throw new ConflictException("Cancelled application cannot be confirmed.");
        }

        var now = DateTimeOffset.UtcNow;

        if (hold.Status == HoldStatus.EXPIRED || hold.ExpiresAt <= now)
        {
            if (hold.Status == HoldStatus.HELD)
            {
                hold.Status = HoldStatus.EXPIRED;
                hold.ExpiredAt = now;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            throw new ConflictException("This application hold has expired.");
        }

        hold.Status = HoldStatus.CONFIRMED;
        hold.ConfirmedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ConfirmHoldResponse(hold.Id, HoldStatus.CONFIRMED);
    }

    public async Task CancelHoldAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var holds = await dbContext.Holds
            .FromSqlInterpolated(
                $"SELECT id, class_id, student_id, status, created_at, expires_at, confirmed_at, cancelled_at, expired_at FROM holds WHERE id = {holdId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        var hold = holds.SingleOrDefault()
            ?? throw new NotFoundException($"Application hold '{holdId}' was not found.");

        if (!string.Equals(hold.StudentId, studentId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("You can only cancel your own application.");
        }

        if (hold.Status == HoldStatus.CONFIRMED)
        {
            throw new ConflictException("Confirmed admission cannot be cancelled.");
        }

        if (hold.Status == HoldStatus.CANCELLED)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        if (hold.Status == HoldStatus.EXPIRED || hold.ExpiresAt <= now)
        {
            if (hold.Status == HoldStatus.HELD)
            {
                hold.Status = HoldStatus.EXPIRED;
                hold.ExpiredAt = now;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            throw new ConflictException("Expired application cannot be cancelled.");
        }

        hold.Status = HoldStatus.CANCELLED;
        hold.CancelledAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MyHoldResponse>> GetStudentHoldsAsync(
        string studentId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var rows = await dbContext.Holds
            .AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                h.Id,
                h.ClassId,
                ClassName = h.AdmissionClass != null ? h.AdmissionClass.Name : string.Empty,
                h.Status,
                h.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new MyHoldResponse(
                r.Id,
                r.ClassId,
                r.ClassName,
                r.Status == HoldStatus.HELD && r.ExpiresAt <= now ? HoldStatus.EXPIRED : r.Status,
                r.ExpiresAt))
            .ToList();
    }

    public Task<int> ExpireOverdueHoldsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return dbContext.Holds
            .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(h => h.Status, HoldStatus.EXPIRED)
                    .SetProperty(h => h.ExpiredAt, now),
                cancellationToken);
    }

    private static string NewId(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}"[..14];
    }
}
