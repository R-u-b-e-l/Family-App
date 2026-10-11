using FamilyManagement.Domain.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Domain.Entities;

/// <summary>Per family + voucher type + fiscal year numbering, incremented inside the posting transaction.</summary>
public class VoucherSequence : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyId { get; set; }
    public VoucherType VoucherType { get; set; }
    public int FiscalYear { get; set; }
    public int NextNumber { get; set; } = 1;
}
