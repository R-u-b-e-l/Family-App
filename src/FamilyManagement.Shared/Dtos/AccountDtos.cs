using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Shared.Dtos;

public class AccountDto
{
    public Guid Id { get; set; }
    public Guid? ParentAccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public AccountType AccountType { get; set; }
    public NormalBalance NormalBalance { get; set; }
    public bool IsGroup { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public CashBankKind CashBankKind { get; set; }
    public int Level { get; set; }
    public string? Description { get; set; }
}

public class AccountTreeNodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public bool IsGroup { get; set; }
    public bool IsActive { get; set; }
    public int Level { get; set; }
    public List<AccountTreeNodeDto> Children { get; set; } = [];
}

public class CreateAccountRequest
{
    public Guid? ParentAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Optional — generated from the parent's range when omitted.</summary>
    public string? Code { get; set; }
    public bool IsGroup { get; set; }
    public string? Description { get; set; }
    /// <summary>Required only for root accounts; children inherit from the parent.</summary>
    public AccountType? AccountType { get; set; }
}

public class UpdateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AccountBalanceDto
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public NormalBalance BalanceSide { get; set; }
    public DateOnly AsOfDate { get; set; }
}
