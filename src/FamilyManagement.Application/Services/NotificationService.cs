using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface INotificationService
{
    /// <summary>Queues notifications; persisted with the caller's SaveChanges.</summary>
    void Notify(Guid familyId, IEnumerable<Guid> userIds, NotificationType type, string title, string? body = null);
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(PagedRequest request, CancellationToken ct = default);
    Task MarkReadAsync(Guid notificationId, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}

public class NotificationService(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock) : INotificationService
{
    public void Notify(Guid familyId, IEnumerable<Guid> userIds, NotificationType type, string title, string? body = null)
    {
        foreach (var userId in userIds.Distinct())
        {
            db.Notifications.Add(new Notification
            {
                FamilyId = familyId,
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                CreatedDate = clock.UtcNow
            });
        }
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = db.Notifications.AsNoTracking()
            .Where(n => n.UserId == currentUser.UserId)
            .OrderByDescending(n => n.CreatedDate);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                IsRead = n.IsRead,
                CreatedDate = n.CreatedDate
            })
            .ToListAsync(ct);

        return new PagedResult<NotificationDto>
        {
            Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total
        };
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        await db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == currentUser.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        await db.Notifications
            .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
