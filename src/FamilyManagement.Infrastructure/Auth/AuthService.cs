using System.Security.Cryptography;
using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.Services;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Infrastructure.Accounting;
using FamilyManagement.Infrastructure.Identity;
using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyManagement.Infrastructure.Auth;

public class AuthService(
    AppDbContext ctx,
    UserManager<AppUser> userManager,
    IJwtTokenService jwt,
    IOptions<JwtOptions> jwtOptions,
    IDateTimeProvider clock,
    IEmailService emailService,
    ISmsService smsService,
    IAuditService audit,
    IPermissionService permissions,
    ICurrentUser currentUser) : IAuthService
{
    private const int OtpExpiryMinutes = 5;
    private const int OtpMaxAttempts = 5;
    private const int OtpResendCooldownSeconds = 60;

    public async Task<AuthResponse> RegisterFamilyAsync(RegisterFamilyRequest request, CancellationToken ct = default)
    {
        await EnsureIdentifierAvailableAsync(request.UserName, request.Email, request.Mobile, ct);

        var userId = await ctx.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;
            var family = new Family { Name = request.FamilyName.Trim(), CreatedDate = now };
            ctx.Families.Add(family);
            ctx.FamilySettings.Add(new FamilySettings { FamilyId = family.Id, CreatedDate = now });

            // Copy the system role templates into this family.
            var templates = await ctx.Roles.AsNoTracking()
                .Where(r => r.FamilyId == null && r.IsSystem)
                .Include(r => r.RolePermissions)
                .ToListAsync(token);

            var roleMap = new Dictionary<Guid, AppRole>();
            foreach (var template in templates)
            {
                var role = new AppRole
                {
                    Id = Guid.NewGuid(),
                    FamilyId = family.Id,
                    Name = template.Name,
                    NormalizedName = template.NormalizedName,
                    Description = template.Description?.Replace(" (template)", ""),
                    IsSystem = true
                };
                ctx.Roles.Add(role);
                foreach (var rp in template.RolePermissions)
                    ctx.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = rp.PermissionId });
                roleMap[template.Id] = role;
            }

            // Default Chart of Accounts + current-year accounting period.
            ctx.Accounts.AddRange(DefaultChartOfAccounts.Build(family.Id, now));
            var year = clock.Today.Year;
            ctx.AccountingPeriods.Add(new AccountingPeriod
            {
                FamilyId = family.Id,
                Name = $"FY {year}",
                StartDate = new DateOnly(year, 1, 1),
                EndDate = new DateOnly(year, 12, 31),
                CreatedDate = now
            });

            var user = new AppUser
            {
                FamilyId = family.Id,
                UserName = request.UserName.Trim(),
                Email = request.Email.Trim(),
                PhoneNumber = request.Mobile.Trim(),
                FullName = request.AdminFullName.Trim(),
                EmailConfirmed = true,
                CreatedDate = now
            };
            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new BadRequestException("Registration failed.", result.Errors.Select(e => e.Description));

            var adminRole = roleMap[SeedData.AdminTemplateRoleId];
            ctx.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = adminRole.Id });

            audit.Log("Family.Register", nameof(Family), family.Id.ToString(), null,
                new { family.Name, Admin = user.UserName }, family.Id, user.Id);
            await ctx.SaveChangesAsync(token);
            return user.Id;
        }, ct);

        var created = await FindUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("Registration failed unexpectedly.");
        return await BuildAuthResponseAsync(created, "Auth.Register", ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await FindUserByIdentifierAsync(request.Identifier, ct)
            ?? throw new UnauthorizedException("Invalid credentials.");

        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException("The account is temporarily locked. Try again later.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedException("Invalid credentials.");
        }

        if (user.Status != EntityStatus.Active)
            throw new UnauthorizedException("The account is inactive. Contact your family admin.");

        var familyActive = await ctx.Families.AsNoTracking()
            .AnyAsync(f => f.Id == user.FamilyId && f.Status == EntityStatus.Active, ct);
        if (!familyActive)
            throw new UnauthorizedException("The family account is inactive.");

        await userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginDate = clock.UtcNow;

        return await BuildAuthResponseAsync(user, "Auth.Login", ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = jwt.Hash(refreshToken);
        var stored = await ctx.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        var now = clock.UtcNow;
        if (stored.RevokedAt is not null)
        {
            // Token reuse: someone replayed a rotated token. Kill the whole session family.
            await ctx.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
            throw new UnauthorizedException("Invalid refresh token.");
        }
        if (stored.IsExpired(now))
            throw new UnauthorizedException("The session has expired. Sign in again.");

        var user = await FindUserByIdAsync(stored.UserId, ct);
        if (user is null || user.Status != EntityStatus.Active)
            throw new UnauthorizedException("The account is no longer active.");

        var (newToken, newHash, entity) = CreateRefreshTokenEntity(user.Id);
        stored.RevokedAt = now;
        stored.RevokedByIp = currentUser.IpAddress;
        stored.ReplacedByTokenHash = newHash;
        ctx.RefreshTokens.Add(entity);
        await ctx.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = jwt.CreateAccessToken(user);
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newToken,
            AccessTokenExpiresAt = expiresAt,
            User = await BuildUserDtoAsync(user, ct),
            Permissions = [.. await permissions.GetUserPermissionsAsync(user.FamilyId, user.Id, ct)]
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = jwt.Hash(refreshToken);
        var stored = await ctx.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = clock.UtcNow;
            stored.RevokedByIp = currentUser.IpAddress;
            audit.Log("Auth.Logout", familyId: currentUser.IsAuthenticated ? currentUser.FamilyId : null,
                userId: stored.UserId);
            await ctx.SaveChangesAsync(ct);
        }
    }

    public async Task ForgotPasswordAsync(string identifier, CancellationToken ct = default)
    {
        var user = await FindUserByIdentifierAsync(identifier, ct);
        if (user is null || user.Status != EntityStatus.Active)
            return; // Never reveal whether the account exists.

        var now = clock.UtcNow;
        var lastOtp = await ctx.OtpCodes
            .Where(o => o.UserId == user.Id && o.Purpose == OtpPurpose.PasswordReset)
            .OrderByDescending(o => o.CreatedDate)
            .FirstOrDefaultAsync(ct);
        if (lastOtp is not null && (now - lastOtp.CreatedDate).TotalSeconds < OtpResendCooldownSeconds)
            throw new ConflictException("Please wait a minute before requesting another code.");

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        ctx.OtpCodes.Add(new OtpCode
        {
            UserId = user.Id,
            Purpose = OtpPurpose.PasswordReset,
            CodeHash = jwt.Hash(otp),
            ExpiresAt = now.AddMinutes(OtpExpiryMinutes),
            CreatedDate = now
        });
        audit.Log("Auth.ForgotPassword", nameof(AppUser), user.Id.ToString(),
            familyId: user.FamilyId, userId: user.Id);
        await ctx.SaveChangesAsync(ct);

        var message = $"Your Family Management password reset code is {otp}. It expires in {OtpExpiryMinutes} minutes.";
        if (identifier.Contains('@') && user.Email is not null)
            await emailService.SendAsync(user.Email, "Password reset code", message, ct);
        else if (user.PhoneNumber is not null)
            await smsService.SendAsync(user.PhoneNumber, message, ct);
        else if (user.Email is not null)
            await emailService.SendAsync(user.Email, "Password reset code", message, ct);
    }

    public async Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        var user = await FindUserByIdentifierAsync(request.Identifier, ct)
            ?? throw new BadRequestException("Invalid or expired code.");

        var now = clock.UtcNow;
        var otp = await ctx.OtpCodes
            .Where(o => o.UserId == user.Id && o.Purpose == OtpPurpose.PasswordReset
                        && o.ConsumedAt == null && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedDate)
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException("Invalid or expired code.");

        if (otp.AttemptCount >= OtpMaxAttempts)
            throw new BadRequestException("Too many attempts. Request a new code.");

        if (otp.CodeHash != jwt.Hash(request.Otp))
        {
            otp.AttemptCount++;
            await ctx.SaveChangesAsync(ct);
            throw new BadRequestException("Invalid or expired code.");
        }

        otp.ConsumedAt = now;
        audit.Log("Auth.OtpVerified", nameof(AppUser), user.Id.ToString(),
            familyId: user.FamilyId, userId: user.Id);
        await ctx.SaveChangesAsync(ct);

        return new VerifyOtpResponse { ResetToken = jwt.CreateResetToken(user) };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var userId = await jwt.ValidateResetTokenAsync(request.ResetToken)
            ?? throw new BadRequestException("The reset link has expired. Start over.");

        var user = await FindUserByIdAsync(userId, ct)
            ?? throw new BadRequestException("The reset link has expired. Start over.");

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, identityToken, request.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException("Password reset failed.", result.Errors.Select(e => e.Description));

        await RevokeAllRefreshTokensAsync(user.Id, ct);
        audit.Log("Auth.PasswordReset", nameof(AppUser), user.Id.ToString(),
            familyId: user.FamilyId, userId: user.Id);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(currentUser.UserId, ct)
            ?? throw new UnauthorizedException("User not found.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException("Password change failed.", result.Errors.Select(e => e.Description));

        await RevokeAllRefreshTokensAsync(user.Id, ct);
        audit.Log("Auth.PasswordChange", nameof(AppUser), user.Id.ToString());
        await ctx.SaveChangesAsync(ct);
    }

    // ---- helpers ---------------------------------------------------------

    private async Task EnsureIdentifierAvailableAsync(string userName, string email, string mobile, CancellationToken ct)
    {
        var normalizedUser = userManager.NormalizeName(userName.Trim());
        var normalizedEmail = userManager.NormalizeEmail(email.Trim());
        var phone = mobile.Trim();

        var errors = new List<string>();
        if (await ctx.Users.IgnoreQueryFilters().AnyAsync(u => u.NormalizedUserName == normalizedUser, ct))
            errors.Add("The username is already taken.");
        if (await ctx.Users.IgnoreQueryFilters().AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
            errors.Add("The email is already registered.");
        if (await ctx.Users.IgnoreQueryFilters().AnyAsync(u => u.PhoneNumber == phone, ct))
            errors.Add("The mobile number is already registered.");
        if (errors.Count > 0)
            throw new ConflictException(string.Join(" ", errors));
    }

    private async Task<AppUser?> FindUserByIdAsync(Guid id, CancellationToken ct) =>
        await ctx.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);

    private async Task<AppUser?> FindUserByIdentifierAsync(string identifier, CancellationToken ct)
    {
        var value = identifier.Trim();
        var normalizedUser = userManager.NormalizeName(value);
        var normalizedEmail = userManager.NormalizeEmail(value);

        return await ctx.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUser
                                      || u.NormalizedEmail == normalizedEmail
                                      || u.PhoneNumber == value, ct);
    }

    private (string Token, string Hash, RefreshToken Entity) CreateRefreshTokenEntity(Guid userId)
    {
        var token = jwt.GenerateRefreshToken();
        var hash = jwt.Hash(token);
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = clock.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays),
            CreatedDate = clock.UtcNow,
            CreatedByIp = currentUser.IpAddress
        };
        return (token, hash, entity);
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        await ctx.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(AppUser user, string auditAction, CancellationToken ct)
    {
        var (token, _, entity) = CreateRefreshTokenEntity(user.Id);
        ctx.RefreshTokens.Add(entity);
        audit.Log(auditAction, nameof(AppUser), user.Id.ToString(), familyId: user.FamilyId, userId: user.Id);
        await ctx.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = jwt.CreateAccessToken(user);
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = token,
            AccessTokenExpiresAt = expiresAt,
            User = await BuildUserDtoAsync(user, ct),
            Permissions = [.. await permissions.GetUserPermissionsAsync(user.FamilyId, user.Id, ct)]
        };
    }

    private async Task<UserDto> BuildUserDtoAsync(AppUser user, CancellationToken ct)
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
