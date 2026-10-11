using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Shared.Dtos;

public class PeriodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public PeriodStatus Status { get; set; }
    public DateTime? ClosedDate { get; set; }
}

public class CreatePeriodRequest
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
