namespace FamilyManagement.Shared.Enums;

public enum EntityStatus
{
    Active = 1,
    Inactive = 2,
    Deleted = 3
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}

public enum NormalBalance
{
    Debit = 1,
    Credit = 2
}

public enum VoucherType
{
    JV = 1,
    PV = 2,
    RV = 3,
    CV = 4
}

public enum VoucherStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3,
    Reversed = 4
}

public enum PeriodStatus
{
    Open = 1,
    Closed = 2
}

/// <summary>Marks accounts that participate in Cash Book / Bank Book reports.</summary>
public enum CashBankKind
{
    None = 0,
    Cash = 1,
    Bank = 2
}

public enum OtpPurpose
{
    PasswordReset = 1
}

public enum NotificationType
{
    MemberAdded = 1,
    PasswordChanged = 2,
    VoucherPosted = 3,
    VoucherCancelled = 4,
    VoucherReversed = 5,
    RoleChanged = 6,
    FamilyNotice = 7,
    SystemUpdate = 8
}
