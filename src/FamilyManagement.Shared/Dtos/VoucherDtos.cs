using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Shared.Dtos;

public class VoucherListItemDto
{
    public Guid Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public DateOnly VoucherDate { get; set; }
    public string? Narration { get; set; }
    public decimal Amount { get; set; }
    public VoucherStatus Status { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}

public class VoucherDto
{
    public Guid Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public DateOnly VoucherDate { get; set; }
    public string? Narration { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public VoucherStatus Status { get; set; }
    public Guid? ReversalOfVoucherId { get; set; }
    public string? ReversalOfVoucherNumber { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? PostedByName { get; set; }
    public DateTime? PostedDate { get; set; }
    public List<VoucherLineDto> Lines { get; set; } = [];
}

public class VoucherLineDto
{
    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Narration { get; set; }
}

public class SaveVoucherRequest
{
    public VoucherType VoucherType { get; set; }
    public DateOnly VoucherDate { get; set; }
    public string? Narration { get; set; }
    public List<SaveVoucherLineRequest> Lines { get; set; } = [];
}

public class SaveVoucherLineRequest
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Narration { get; set; }
}

public class VoucherFilterRequest : PagedRequest
{
    public VoucherType? VoucherType { get; set; }
    public VoucherStatus? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
