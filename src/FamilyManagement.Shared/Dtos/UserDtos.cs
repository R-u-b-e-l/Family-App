using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Shared.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public EntityStatus Status { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public DateTime? LastLoginDate { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateMemberRequest
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string Password { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public List<Guid> RoleIds { get; set; } = [];
}

public class UpdateMemberRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
}

public class UpdateMemberStatusRequest
{
    public EntityStatus Status { get; set; }
}

public class AssignRolesRequest
{
    public List<Guid> RoleIds { get; set; } = [];
}

public class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
