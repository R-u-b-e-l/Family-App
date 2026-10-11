using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class AccountEntryViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _isGroup;
    [ObservableProperty] private AccountDto? _selectedParent;

    public ObservableCollection<AccountDto> ParentOptions { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var accounts = await api.GetAccountsAsync(pageSize: 100);
        ParentOptions.Clear();
        foreach (var account in accounts.Items.Where(a => a.IsGroup))
            ParentOptions.Add(account);
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        if (SelectedParent is null)
        {
            await ToastAsync("Choose a parent group account.");
            return;
        }
        if (string.IsNullOrWhiteSpace(Name))
        {
            await ToastAsync("Enter the account name.");
            return;
        }

        await api.CreateAccountAsync(new CreateAccountRequest
        {
            ParentAccountId = SelectedParent.Id,
            Name = Name.Trim(),
            Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim(),
            IsGroup = IsGroup,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
        });

        await ToastAsync("Account created");
        await Shell.Current.GoToAsync("..");
    });
}
