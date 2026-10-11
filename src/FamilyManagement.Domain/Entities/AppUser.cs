using FamilyManagement.Domain.Common;
using FamilyManagement.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace FamilyManagement.Domain.Entities;

public class AppUser : IdentityUser<Guid>, ITenantEntity
{
    public Guid FamilyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? ProfilePicturePath { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }

    public Family? Family { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
