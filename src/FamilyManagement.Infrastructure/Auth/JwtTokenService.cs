using System.Security.Cryptography;
using System.Text;
using FamilyManagement.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FamilyManagement.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "FamilyManagement";
    public string Audience { get; set; } = "FamilyManagement.Maui";
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
    public int ResetTokenMinutes { get; set; } = 10;
}

public static class AppClaimTypes
{
    public const string FamilyId = "family_id";
    public const string FullName = "full_name";
    public const string Purpose = "purpose";
    public const string PasswordResetPurpose = "password_reset";
}

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(AppUser user);
    string CreateResetToken(AppUser user);
    /// <summary>Returns the user id if the reset token is valid, otherwise null.</summary>
    Task<Guid?> ValidateResetTokenAsync(string token);
    string GenerateRefreshToken();
    string Hash(string value);
}

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    private SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(_options.Secret));

    public (string Token, DateTime ExpiresAt) CreateAccessToken(AppUser user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                [JwtRegisteredClaimNames.Name] = user.UserName ?? "",
                [AppClaimTypes.FamilyId] = user.FamilyId.ToString(),
                [AppClaimTypes.FullName] = user.FullName
            },
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });

        return (token, expires);
    }

    public string CreateResetToken(AppUser user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_options.ResetTokenMinutes),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                [AppClaimTypes.Purpose] = AppClaimTypes.PasswordResetPurpose
            },
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }

    public async Task<Guid?> ValidateResetTokenAsync(string token)
    {
        var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = SigningKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        });

        if (!result.IsValid)
            return null;
        result.Claims.TryGetValue(AppClaimTypes.Purpose, out var purpose);
        if (purpose as string != AppClaimTypes.PasswordResetPurpose)
            return null;
        result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub);
        return Guid.TryParse(sub as string, out var id) ? id : null;
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string Hash(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
