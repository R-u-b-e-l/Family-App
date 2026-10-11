using FamilyManagement.Domain.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Domain.Entities;

public class Notification : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
}
