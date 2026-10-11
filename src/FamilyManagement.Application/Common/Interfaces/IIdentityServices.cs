using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Application.Common.Interfaces;

/// <summary>Implemented in Infrastructure (depends on ASP.NET Core Identity).</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterFamilyAsync(RegisterFamilyRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task ForgotPasswordAsync(string identifier, CancellationToken ct = default);
    Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

/// <summary>Implemented in Infrastructure (depends on UserManager).</summary>
public interface IUserService
{
    Task<PagedResult<UserDto>> GetMembersAsync(PagedRequest request, CancellationToken ct = default);
    Task<UserDto> GetMemberAsync(Guid userId, CancellationToken ct = default);
    Task<UserDto> CreateMemberAsync(CreateMemberRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateMemberAsync(Guid userId, UpdateMemberRequest request, CancellationToken ct = default);
    Task SetMemberStatusAsync(Guid userId, UpdateMemberStatusRequest request, CancellationToken ct = default);
    Task AssignRolesAsync(Guid userId, AssignRolesRequest request, CancellationToken ct = default);
    Task AdminResetPasswordAsync(Guid userId, AdminResetPasswordRequest request, CancellationToken ct = default);
    Task<UserDto> GetProfileAsync(CancellationToken ct = default);
    Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    /// <summary>Persists the stored picture's relative path on the signed-in user and returns the old path (for cleanup).</summary>
    Task<(UserDto User, string? OldPath)> SetProfilePicturePathAsync(string relativePath, CancellationToken ct = default);
}

/// <summary>Implemented in Infrastructure (depends on RoleManager).</summary>
public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleDto> GetRoleAsync(Guid roleId, CancellationToken ct = default);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken ct = default);
    Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default);
    Task UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct = default);
    Task<List<PermissionDto>> GetPermissionCatalogAsync(CancellationToken ct = default);
}

public interface IPermissionService
{
    /// <summary>Effective permission codes for a user (union over roles), cached per family+user.</summary>
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid familyId, Guid userId, CancellationToken ct = default);
    void InvalidateUser(Guid familyId, Guid userId);
    void InvalidateFamily(Guid familyId);
}
