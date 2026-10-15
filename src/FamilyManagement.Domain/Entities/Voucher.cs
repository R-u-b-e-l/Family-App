using FamilyManagement.Domain.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Domain.Entities;

public class Voucher : AuditableEntity, ITenantEntity
{
    public Guid FamilyId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public DateOnly VoucherDate { get; set; }
    public Guid PeriodId { get; set; }
    public string? Narration { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public VoucherStatus Status { get; set; } = VoucherStatus.Draft;
    /// <summary>Set on the mirror voucher that reverses a posted voucher.</summary>
    public Guid? ReversalOfVoucherId { get; set; }
    /// <summary>Optional member attribution: who contributed / spent / took the loan.</summary>
    public Guid? MemberId { get; set; }
    public Guid? PostedBy { get; set; }
    public DateTime? PostedDate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public AccountingPeriod? Period { get; set; }
    public Voucher? ReversalOfVoucher { get; set; }
    public ICollection<VoucherDetail> Details { get; set; } = [];

    public bool IsBalanced() =>
        Details.Count >= 2
        && Details.Sum(d => d.Debit) == Details.Sum(d => d.Credit)
        && Details.Sum(d => d.Debit) > 0
        && Details.All(d => (d.Debit > 0) ^ (d.Credit > 0));
}
