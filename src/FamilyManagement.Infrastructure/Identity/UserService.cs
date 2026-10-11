using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.Services;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Identity;

public class UserService(
    AppDbContext ctx,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    IAuditService audit,
    INotificationService notifications,
    IPermissionService permissions,
    IDateTimeProvider clock) : IUserService
{
    public async Task<PagedResult<UserDto>> GetMembersAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = ctx.Users.AsNoTracking().Where(u => u.Status != EntityStatus.Deleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u => u.FullName.Contains(term)
                                     || u.UserName!.Contains(term)
                                     || (u.Email != null && u.Email.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FamilyId = u.FamilyId,
                UserName = u.UserName ?? "",
                Email = u.Email,
                Mobile = u.PhoneNumber,
                FullName = u.FullName,
                DateOfBirth = u.DateOfBirth,
                Gender = u.Gender,
                Address = u.Address,
                ProfilePictureUrl = u.ProfilePicturePath,
                Status = u.Status,
                FamilyName = u.Family!.Name,
                LastLoginDate = u.LastLoginDate,
                CreatedDate = u.CreatedDate,
                Roles = (from ur in ctx.UserRoles
                         join r in ctx.Roles on ur.RoleId equals r.Id
                         where ur.UserId == u.Id
                         select r.Name!).ToList()
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<UserDto> GetMemberAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Member not found.");
        return await MapAsync(user, ct);
    }

    public async Task<UserDto> CreateMemberAsync(CreateMemberRequest request, CancellationToken ct = default)
    {
        await EnsureAvailableAsync(request.UserName, request.Email, request.Mobile, excludeUserId: null, ct);
        var roles = await ResolveFamilyRolesAsync(request.RoleIds, ct);

        var user = new AppUser
        {
            FamilyId = currentUser.FamilyId,
            UserName = request.UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim(),
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            EmailConfirmed = true,
            CreatedDate = clock.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new BadRequestException("Member creation failed.", result.Errors.Select(e => e.Description));

        // Default to FamilyMember when no role was chosen.
        if (roles.Count == 0)
        {
            var memberRole = await ctx.Roles
                .FirstOrDefaultAsync(r => r.FamilyId == currentUser.FamilyId && r.Name == SeedData.FamilyMemberRoleName, ct);
            if (memberRole is not null)
                roles.Add(memberRole);
        }
        foreach (var role in roles)
            ctx.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });

        audit.Log("Member.Create", nameof(AppUser), user.Id.ToString(), null, new { user.UserName, user.FullName });
        await NotifyActiveMembersAsync(NotificationType.MemberAdded,
            $"{user.FullName} joined the family", excludeUserId: user.Id, ct);
        await ctx.SaveChangesAsync(ct);

        return await MapAsync(user, ct);
    }

    public async Task<UserDto> UpdateMemberAsync(Guid userId, UpdateMemberRequest request, CancellationToken ct = default)
    {
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Member not found.");
        await EnsureAvailableAsync(null, request.Email, request.Mobile, excludeUserId: userId, ct);

        var old = new { user.FullName, user.Email, Mobile = user.PhoneNumber };
        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.NormalizedEmail = user.Email is null ? null : userManager.NormalizeEmail(user.Email);
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender;
        user.Address = request.Address;
        user.UpdatedDate = clock.UtcNow;

        audit.Log("Member.Update", nameof(AppUser), user.Id.ToString(), old,
            new { user.FullName, user.Email, Mobile = user.PhoneNumber });
        await ctx.SaveChangesAsync(ct);
        return await MapAsync(user, ct);
    }

    public async Task SetMemberStatusAsync(Guid userId, UpdateMemberStatusRequest request, CancellationToken ct = default)
    {
        if (userId == currentUser.UserId)
            throw new BadRequestException("You cannot change your own status.");

        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Member not found.");

        user.Status = request.Status;
        user.UpdatedDate = clock.UtcNow;

        if (request.Status != EntityStatus.Active)
        {
            var now = clock.UtcNow;
            await ctx.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
            await userManager.UpdateSecurityStampAsync(user);
        }

        audit.Log("Member.StatusChange", nameof(AppUser), user.Id.ToString(), null, new { request.Status });
        await ctx.SaveChangesAsync(ct);
    }

    public async Task AssignRolesAsync(Guid userId, AssignRolesRequest request, CancellationToken ct = default)
    {
        var user = await ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Member not found.");
        var roles = await ResolveFamilyRolesAsync(request.RoleIds, ct);
        if (roles.Count == 0)
            throw new BadRequestException("At least one role is required.");

        // Never leave the family without an admin.
        var adminRoleId = await ctx.Roles
            .Where(r => r.FamilyId == currentUser.FamilyId && r.Name == SeedData.FamilyAdminRoleName)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);
        if (adminRoleId != Guid.Empty && !request.RoleIds.Contains(adminRoleId))
        {
            var otherAdmins = await ctx.UserRoles
                .Join(ctx.Users, ur => ur.UserId, u => u.Id, (ur, u) => new { ur, u })
                .CountAsync(x => x.ur.RoleId == adminRoleId && x.ur.UserId != userId
                                 && x.u.Status == EntityStatus.Active, ct);
            var isCurrentlyAdmin = await ctx.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRoleId, ct);
            if (isCurrentlyAdmin && otherAdmins == 0)
                throw new ConflictException("The family must keep at least one active admin.");
        }

        var existing = await ctx.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(ct);
        ctx.UserRoles.RemoveRange(existing);
        foreach (var role in roles)
            ctx.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = role.Id });

        audit.Log("Member.RoleChange", nameof(AppUser), userId.ToString(),
            new { Old = existing.Select(e => e.RoleId) }, new { New = roles.Select(r => r.Name) });
        notifications.Notify(currentUser.FamilyId, [userId], NotificationType.RoleChanged,
            "Your roles were updated", $"New roles: {string.Join(", ", roles.Select(r => r.Name))}");
        await ctx.SaveChangesAsync(ct);

        permissions.InvalidateUser(currentUser.FamilyId, userId);
    }

    public async Task AdminResetPasswordAsync(Guid userId, AdminResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Member not found.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException("Password reset failed.", result.Errors.Select(e => e.Description));

        var now = clock.UtcNow;
        await ctx.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

        audit.Log("Member.PasswordReset", nameof(AppUser), user.Id.ToString());
        notifications.Notify(currentUser.FamilyId, [userId], NotificationType.PasswordChanged,
            "Your password was reset by the family admin");
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetProfileAsync(CancellationToken ct = default)
    {
        var user = await ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException("Profile not found.");
        return await MapAsync(user, ct);
    }

    public async Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException("Profile not found.");
        await EnsureAvailableAsync(null, request.Email, request.Mobile, excludeUserId: user.Id, ct);

        var old = new { user.FullName, user.Email, Mobile = user.PhoneNumber, user.Address };
        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.NormalizedEmail = user.Email is null ? null : userManager.NormalizeEmail(user.Email);
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender;
        user.Address = request.Address;
        user.UpdatedDate = clock.UtcNow;

        audit.Log("Profile.Update", nameof(AppUser), user.Id.ToString(), old,
            new { user.FullName, user.Email, Mobile = user.PhoneNumber, user.Address });
        await ctx.SaveChangesAsync(ct);
        return await MapAsync(user, ct);
    }

    public async Task<(UserDto User, string? OldPath)> SetProfilePicturePathAsync(string relativePath, CancellationToken ct = default)
    {
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException("Profile not found.");

        var oldPath = user.ProfilePicturePath;
        user.ProfilePicturePath = relativePath;
        user.UpdatedDate = clock.UtcNow;

        audit.Log("Profile.PictureChange", nameof(AppUser), user.Id.ToString());
        await ctx.SaveChangesAsync(ct);
        return (await MapAsync(user, ct), oldPath);
    }

    // ---- helpers ---------------------------------------------------------

    private async Task EnsureAvailableAsync(string? userName, string? email, string? mobile, Guid? excludeUserId, CancellationToken ct)
    {
        var errors = new List<string>();
        var users = ctx.Users.IgnoreQueryFilters().Where(u => excludeUserId == null || u.Id != excludeUserId);

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var normalized = userManager.NormalizeName(userName.Trim());
            if (await users.AnyAsync(u => u.NormalizedUserName == normalized, ct))
                errors.Add("The username is already taken.");
        }
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = userManager.NormalizeEmail(email.Trim());
            if (await users.AnyAsync(u => u.NormalizedEmail == normalized, ct))
                errors.Add("The email is already registered.");
        }
        if (!string.IsNullOrWhiteSpace(mobile))
        {
            var phone = mobile.Trim();
            if (await users.AnyAsync(u => u.PhoneNumber == phone, ct))
                errors.Add("The mobile number is already registered.");
        }
        if (errors.Count > 0)
            throw new ConflictException(string.Join(" ", errors));
    }

    private async Task<List<AppRole>> ResolveFamilyRolesAsync(List<Guid> roleIds, CancellationToken ct)
    {
        if (roleIds.Count == 0)
            return [];
        var roles = await ctx.Roles
            .Where(r => r.FamilyId == currentUser.FamilyId && roleIds.Contains(r.Id))
            .ToListAsync(ct);
        if (roles.Count != roleIds.Distinct().Count())
            throw new BadRequestException("One or more roles were not found.");
        return roles;
    }

    private async Task NotifyActiveMembersAsync(NotificationType type, string title, Guid excludeUserId, CancellationToken ct)
    {
        var recipients = await ctx.Users.AsNoTracking()
            .Where(u => u.Status == EntityStatus.Active && u.Id != currentUser.UserId && u.Id != excludeUserId)
            .Select(u => u.Id)
            .ToListAsync(ct);
        notifications.Notify(currentUser.FamilyId, recipients, type, title);
    }

    private async Task<UserDto> MapAsync(AppUser user, CancellationToken ct)
    {
        var roles = await (from ur in ctx.UserRoles
                           join r in ctx.Roles on ur.RoleId equals r.Id
                           where ur.UserId == user.Id
                           select r.Name!).ToListAsync(ct);
        var familyName = await ctx.Families.AsNoTracking()
            .Where(f => f.Id == user.FamilyId)
            .Select(f => f.Name)
            .FirstOrDefaultAsync(ct) ?? "";
        return UserDtoMapper.Map(user, roles, familyName);
    }
}
