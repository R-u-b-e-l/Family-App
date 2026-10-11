using FamilyManagement.Domain.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Domain.Entities;

public class AccountingPeriod : AuditableEntity, ITenantEntity
{
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public PeriodStatus Status { get; set; } = PeriodStatus.Open;
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedDate { get; set; }

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;
}
