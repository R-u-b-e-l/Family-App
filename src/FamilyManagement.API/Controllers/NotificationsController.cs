using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class NotificationsController(INotificationService notificationService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetMine([FromQuery] PagedRequest request, CancellationToken ct) =>
        Ok(Success(await notificationService.GetMyNotificationsAsync(request, ct)));

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse>> MarkRead(Guid id, CancellationToken ct)
    {
        await notificationService.MarkReadAsync(id, ct);
        return Ok(Success());
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<ApiResponse>> MarkAllRead(CancellationToken ct)
    {
        await notificationService.MarkAllReadAsync(ct);
        return Ok(Success());
    }
}

public class AuditLogsController(IAuditService auditService) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.AuditLogView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken ct) =>
        Ok(Success(await auditService.GetPagedAsync(request, ct)));
}
