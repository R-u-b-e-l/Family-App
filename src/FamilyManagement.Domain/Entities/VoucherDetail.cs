namespace FamilyManagement.Domain.Entities;

public class VoucherDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VoucherId { get; set; }
    public int LineNo { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Narration { get; set; }

    public Voucher? Voucher { get; set; }
    public Account? Account { get; set; }
}
