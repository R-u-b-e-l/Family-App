using System.Text.Json;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IAuditService
{
    /// <summary>Queues an audit entry; it is persisted with the caller's SaveChanges/transaction.</summary>
    void Log(string action, string? entityName = null, string? entityId = null,
        object? oldValue = null, object? newValue = null, Guid? familyId = null, Guid? userId = null);

    Task<PagedResult<AuditLogDto>> GetPagedAsync(PagedRequest request, CancellationToken ct = default);
}

public class AuditService(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public void Log(string action, string? entityName = null, string? entityId = null,
        object? oldValue = null, object? newValue = null, Guid? familyId = null, Guid? userId = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId ?? (currentUser.IsAuthenticated ? currentUser.FamilyId : Guid.Empty),
            UserId = userId ?? (currentUser.IsAuthenticated ? currentUser.UserId : null),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValueJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
            NewValueJson = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
            IpAddress = currentUser.IpAddress,
            DeviceInfo = currentUser.DeviceInfo,
            CreatedDate = clock.UtcNow
        });
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a => a.Action.Contains(term)
                                     || (a.EntityName != null && a.EntityName.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserName = db.Users.Where(u => u.Id == a.UserId).Select(u => u.FullName).FirstOrDefault(),
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                IpAddress = a.IpAddress,
                CreatedDate = a.CreatedDate
            })
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total };
    }
}
