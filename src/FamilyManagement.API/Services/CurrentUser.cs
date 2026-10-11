using System.Security.Claims;
using FamilyManagement.Application.Common.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FamilyManagement.API.Services;

/// <summary>Resolves the caller's identity from the validated JWT on the current request.</summary>
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : Guid.Empty;

    public Guid FamilyId =>
        Guid.TryParse(Principal?.FindFirstValue("family_id"), out var id) ? id : Guid.Empty;

    public string? UserName =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Name) ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? DeviceInfo
    {
        get
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrEmpty(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 500)];
        }
    }
}
