using Microsoft.AspNetCore.Identity;

namespace FamilyManagement.Domain.Entities;

/// <summary>
/// Roles are scoped per family. FamilyId == null marks a system template role
/// which is copied into each new family at registration.
/// </summary>
public class AppRole : IdentityRole<Guid>
{
    public Guid? FamilyId { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
