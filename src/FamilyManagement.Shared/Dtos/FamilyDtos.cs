namespace FamilyManagement.Shared.Dtos;

public class FamilyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class UpdateFamilyRequest
{
    public string Name { get; set; } = string.Empty;
}

public class FamilySettingsDto
{
    public string Currency { get; set; } = "BDT";
    public int FinancialYearStartMonth { get; set; } = 1;
    public string DateFormat { get; set; } = "dd-MMM-yyyy";
    public string Timezone { get; set; } = "Asia/Dhaka";
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
}

public class UpdateFamilySettingsRequest : FamilySettingsDto;
