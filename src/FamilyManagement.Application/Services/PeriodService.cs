using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IPeriodService
{
    Task<List<PeriodDto>> GetPeriodsAsync(CancellationToken ct = default);
    Task<PeriodDto> CreatePeriodAsync(CreatePeriodRequest request, CancellationToken ct = default);
    Task<PeriodDto> ClosePeriodAsync(Guid periodId, CancellationToken ct = default);
    Task<PeriodDto> ReopenPeriodAsync(Guid periodId, CancellationToken ct = default);
}

public class PeriodService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IAuditService audit,
    IDateTimeProvider clock) : IPeriodService
{
    public async Task<List<PeriodDto>> GetPeriodsAsync(CancellationToken ct = default) =>
        await db.AccountingPeriods.AsNoTracking()
            .OrderByDescending(p => p.StartDate)
            .Select(p => new PeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                ClosedDate = p.ClosedDate
            })
            .ToListAsync(ct);

    public async Task<PeriodDto> CreatePeriodAsync(CreatePeriodRequest request, CancellationToken ct = default)
    {
        if (request.EndDate <= request.StartDate)
            throw new BadRequestException("End date must be after start date.");

        var overlaps = await db.AccountingPeriods
            .AnyAsync(p => p.StartDate <= request.EndDate && p.EndDate >= request.StartDate, ct);
        if (overlaps)
            throw new ConflictException("The date range overlaps an existing accounting period.");

        var period = new AccountingPeriod
        {
            FamilyId = currentUser.FamilyId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = PeriodStatus.Open,
            CreatedBy = currentUser.UserId,
            CreatedDate = clock.UtcNow
        };

        db.AccountingPeriods.Add(period);
        audit.Log("Period.Create", nameof(AccountingPeriod), period.Id.ToString(), null,
            new { period.Name, period.StartDate, period.EndDate });
        await db.SaveChangesAsync(ct);

        return ToDto(period);
    }

    public async Task<PeriodDto> ClosePeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await GetOwnedPeriodAsync(periodId, ct);
        if (period.Status == PeriodStatus.Closed)
            throw new ConflictException("The period is already closed.");

        period.Status = PeriodStatus.Closed;
        period.ClosedBy = currentUser.UserId;
        period.ClosedDate = clock.UtcNow;
        period.UpdatedBy = currentUser.UserId;
        period.UpdatedDate = clock.UtcNow;

        audit.Log("Period.Close", nameof(AccountingPeriod), period.Id.ToString());
        await db.SaveChangesAsync(ct);
        return ToDto(period);
    }

    public async Task<PeriodDto> ReopenPeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await GetOwnedPeriodAsync(periodId, ct);
        if (period.Status == PeriodStatus.Open)
            throw new ConflictException("The period is already open.");

        period.Status = PeriodStatus.Open;
        period.ClosedBy = null;
        period.ClosedDate = null;
        period.UpdatedBy = currentUser.UserId;
        period.UpdatedDate = clock.UtcNow;

        audit.Log("Period.Reopen", nameof(AccountingPeriod), period.Id.ToString());
        await db.SaveChangesAsync(ct);
        return ToDto(period);
    }

    private async Task<AccountingPeriod> GetOwnedPeriodAsync(Guid periodId, CancellationToken ct) =>
        await db.AccountingPeriods.FirstOrDefaultAsync(p => p.Id == periodId, ct)
            ?? throw new NotFoundException("Accounting period not found.");

    private static PeriodDto ToDto(AccountingPeriod p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Status = p.Status,
        ClosedDate = p.ClosedDate
    };
}
