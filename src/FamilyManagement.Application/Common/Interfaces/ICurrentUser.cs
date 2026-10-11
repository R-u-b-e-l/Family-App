namespace FamilyManagement.Application.Common.Interfaces;

/// <summary>Identity of the caller, resolved from the validated JWT. Never taken from request payloads.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    Guid FamilyId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    string? DeviceInfo { get; }
}
