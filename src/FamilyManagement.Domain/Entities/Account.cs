using FamilyManagement.Domain.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Domain.Entities;

public class Account : AuditableEntity, ITenantEntity
{
    public Guid FamilyId { get; set; }
    public Guid? ParentAccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public NormalBalance NormalBalance { get; set; }
    /// <summary>Group accounts structure the tree; only non-group (leaf) accounts accept postings.</summary>
    public bool IsGroup { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Inherited from the parent at creation; drives Cash Book / Bank Book membership.</summary>
    public CashBankKind CashBankKind { get; set; } = CashBankKind.None;
    public int Level { get; set; }
    public string? Description { get; set; }

    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = [];
}
