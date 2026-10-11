using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Infrastructure.Accounting;

/// <summary>Builds the default Chart of Accounts created for every new family.</summary>
public static class DefaultChartOfAccounts
{
    public static List<Account> Build(Guid familyId, DateTime utcNow)
    {
        var accounts = new List<Account>();

        Account Add(string code, string name, AccountType type, Account? parent,
            bool isGroup, bool isSystem = false, CashBankKind kind = CashBankKind.None)
        {
            var account = new Account
            {
                FamilyId = familyId,
                ParentAccountId = parent?.Id,
                Code = code,
                Name = name,
                AccountType = type,
                NormalBalance = type is AccountType.Asset or AccountType.Expense
                    ? NormalBalance.Debit : NormalBalance.Credit,
                IsGroup = isGroup,
                IsSystem = isSystem,
                CashBankKind = kind != CashBankKind.None ? kind : parent?.CashBankKind ?? CashBankKind.None,
                Level = parent is null ? 0 : parent.Level + 1,
                CreatedDate = utcNow
            };
            accounts.Add(account);
            return account;
        }

        // Assets
        var assets = Add("1000", "Assets", AccountType.Asset, null, isGroup: true, isSystem: true);
        var currentAssets = Add("1100", "Current Assets", AccountType.Asset, assets, isGroup: true, isSystem: true);
        var cash = Add("1110", "Cash", AccountType.Asset, currentAssets, isGroup: true, isSystem: true, kind: CashBankKind.Cash);
        Add("1111", "Cash in Hand", AccountType.Asset, cash, isGroup: false, isSystem: true);
        var bank = Add("1120", "Bank", AccountType.Asset, currentAssets, isGroup: true, isSystem: true, kind: CashBankKind.Bank);
        Add("1121", "Primary Bank Account", AccountType.Asset, bank, isGroup: false);
        Add("1130", "Accounts Receivable", AccountType.Asset, currentAssets, isGroup: false);
        var fixedAssets = Add("1200", "Fixed Assets", AccountType.Asset, assets, isGroup: true, isSystem: true);
        Add("1210", "Property", AccountType.Asset, fixedAssets, isGroup: false);
        Add("1220", "Vehicle", AccountType.Asset, fixedAssets, isGroup: false);
        Add("1230", "Equipment", AccountType.Asset, fixedAssets, isGroup: false);

        // Liabilities
        var liabilities = Add("2000", "Liabilities", AccountType.Liability, null, isGroup: true, isSystem: true);
        var currentLiabilities = Add("2100", "Current Liabilities", AccountType.Liability, liabilities, isGroup: true, isSystem: true);
        Add("2110", "Accounts Payable", AccountType.Liability, currentLiabilities, isGroup: false);
        Add("2120", "Loans", AccountType.Liability, currentLiabilities, isGroup: false);

        // Equity
        var equity = Add("3000", "Equity", AccountType.Equity, null, isGroup: true, isSystem: true);
        Add("3100", "Family Capital", AccountType.Equity, equity, isGroup: false, isSystem: true);
        Add("3200", "Retained Earnings", AccountType.Equity, equity, isGroup: false, isSystem: true);

        // Income
        var income = Add("4000", "Income", AccountType.Income, null, isGroup: true, isSystem: true);
        Add("4100", "Salary Income", AccountType.Income, income, isGroup: false);
        Add("4200", "Business Income", AccountType.Income, income, isGroup: false);
        Add("4900", "Other Income", AccountType.Income, income, isGroup: false);

        // Expenses
        var expenses = Add("5000", "Expenses", AccountType.Expense, null, isGroup: true, isSystem: true);
        Add("5100", "Food Expense", AccountType.Expense, expenses, isGroup: false);
        Add("5200", "Education Expense", AccountType.Expense, expenses, isGroup: false);
        Add("5300", "Medical Expense", AccountType.Expense, expenses, isGroup: false);
        Add("5400", "Utility Expense", AccountType.Expense, expenses, isGroup: false);
        Add("5500", "House Rent", AccountType.Expense, expenses, isGroup: false);
        Add("5600", "Transportation", AccountType.Expense, expenses, isGroup: false);
        Add("5900", "Other Expense", AccountType.Expense, expenses, isGroup: false);

        return accounts;
    }
}
