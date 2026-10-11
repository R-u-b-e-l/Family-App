namespace FamilyManagement.Domain.Common;

/// <summary>
/// Marks an entity as family-owned. Global query filters and the tenant write
/// interceptor are applied to every entity implementing this interface.
/// </summary>
public interface ITenantEntity
{
    Guid FamilyId { get; set; }
}
