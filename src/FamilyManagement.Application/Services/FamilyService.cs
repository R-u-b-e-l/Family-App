using FamilyManagement.Application.Common;
using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IFamilyService
{
    Task<FamilyDto> GetCurrentFamilyAsync(CancellationToken ct = default);
    Task<FamilyDto> UpdateFamilyAsync(UpdateFamilyRequest request, CancellationToken ct = default);
    Task<FamilySettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<FamilySettingsDto> UpdateSettingsAsync(UpdateFamilySettingsRequest request, CancellationToken ct = default);
}

public class FamilyService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ICacheService cache,
    IAuditService audit,
    IDateTimeProvider clock) : IFamilyService
{
    public async Task<FamilyDto> GetCurrentFamilyAsync(CancellationToken ct = default)
    {
        var family = await db.Families.AsNoTracking()
            .Where(f => f.Id == currentUser.FamilyId)
            .Select(f => new FamilyDto
            {
                Id = f.Id,
                Name = f.Name,
                LogoUrl = f.LogoPath,
                CreatedDate = f.CreatedDate,
                TotalMembers = f.Users.Count(u => u.Status != EntityStatus.Deleted),
                ActiveMembers = f.Users.Count(u => u.Status == EntityStatus.Active)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Family not found.");

        return family;
    }

    public async Task<FamilyDto> UpdateFamilyAsync(UpdateFamilyRequest request, CancellationToken ct = default)
    {
        var family = await db.Families.FirstOrDefaultAsync(f => f.Id == currentUser.FamilyId, ct)
            ?? throw new NotFoundException("Family not found.");

        var oldName = family.Name;
        family.Name = request.Name.Trim();
        family.UpdatedDate = clock.UtcNow;

        audit.Log("Family.Update", nameof(Family), family.Id.ToString(),
            new { Name = oldName }, new { family.Name });
        await db.SaveChangesAsync(ct);

        return await GetCurrentFamilyAsync(ct);
    }

    public async Task<FamilySettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.FamilySettings(currentUser.FamilyId),
            TimeSpan.FromHours(1),
            async token =>
            {
                var s = await db.FamilySettings.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FamilyId == currentUser.FamilyId, token)
                    ?? throw new NotFoundException("Family settings not found.");

                return new FamilySettingsDto
                {
                    Currency = s.Currency,
                    FinancialYearStartMonth = s.FinancialYearStartMonth,
                    DateFormat = s.DateFormat,
                    Timezone = s.Timezone,
                    Address = s.Address,
                    ContactPhone = s.ContactPhone,
                    ContactEmail = s.ContactEmail
                };
            }, ct);
    }

    public async Task<FamilySettingsDto> UpdateSettingsAsync(UpdateFamilySettingsRequest request, CancellationToken ct = default)
    {
        var settings = await db.FamilySettings
            .FirstOrDefaultAsync(x => x.FamilyId == currentUser.FamilyId, ct)
            ?? throw new NotFoundException("Family settings not found.");

        var old = new { settings.Currency, settings.FinancialYearStartMonth, settings.DateFormat, settings.Timezone };

        settings.Currency = request.Currency.Trim().ToUpperInvariant();
        settings.FinancialYearStartMonth = request.FinancialYearStartMonth;
        settings.DateFormat = request.DateFormat;
        settings.Timezone = request.Timezone;
        settings.Address = request.Address;
        settings.ContactPhone = request.ContactPhone;
        settings.ContactEmail = request.ContactEmail;
        settings.UpdatedDate = clock.UtcNow;

        audit.Log("Settings.Update", nameof(FamilySettings), settings.Id.ToString(), old,
            new { settings.Currency, settings.FinancialYearStartMonth, settings.DateFormat, settings.Timezone });
        await db.SaveChangesAsync(ct);

        cache.Remove(CacheKeys.FamilySettings(currentUser.FamilyId));
        return await GetSettingsAsync(ct);
    }
}
