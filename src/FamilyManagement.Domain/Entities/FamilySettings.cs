using FamilyManagement.Domain.Common;

namespace FamilyManagement.Domain.Entities;

public class FamilySettings : BaseEntity, ITenantEntity
{
    public Guid FamilyId { get; set; }
    public string Currency { get; set; } = "BDT";
    public int FinancialYearStartMonth { get; set; } = 1;
    public string DateFormat { get; set; } = "dd-MMM-yyyy";
    public string Timezone { get; set; } = "Asia/Dhaka";
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    public Family? Family { get; set; }
}
