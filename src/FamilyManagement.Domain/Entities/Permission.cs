namespace FamilyManagement.Domain.Entities;

/// <summary>Global permission catalog. Seeded; new modules add rows via migration.</summary>
public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public int PermissionId { get; set; }

    public AppRole? Role { get; set; }
    public Permission? Permission { get; set; }
}
