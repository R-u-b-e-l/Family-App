using FamilyManagement.Domain.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Domain.Entities;

public class Family : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public FamilySettings? Settings { get; set; }
    public ICollection<AppUser> Users { get; set; } = [];
}
