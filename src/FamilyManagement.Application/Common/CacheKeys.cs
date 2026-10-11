namespace FamilyManagement.Application.Common;

/// <summary>Every key is prefixed with the family id so cached data can never cross tenants.</summary>
public static class CacheKeys
{
    public static string Permissions(Guid familyId, Guid userId) => $"perm:{familyId}:{userId}";
    public static string ChartOfAccounts(Guid familyId) => $"coa:{familyId}";
    public static string FamilySettings(Guid familyId) => $"settings:{familyId}";
    public static string Dashboard(Guid familyId) => $"dash:{familyId}";
}
