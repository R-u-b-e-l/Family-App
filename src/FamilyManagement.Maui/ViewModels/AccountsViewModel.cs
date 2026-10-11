using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class AccountRow : ObservableObject
{
    public required AccountTreeNodeDto Node { get; init; }
    public Thickness Indent => new(Node.Level * 18, 0, 0, 0);
    public bool IsGroup => Node.IsGroup;
    public string Display => $"{Node.Code}  {Node.Name}";
}

public partial class AccountsViewModel(ApiClient api, Session session, LocalCache cache) : BaseViewModel
{
    [ObservableProperty] private string _search = string.Empty;

    public ObservableCollection<AccountRow> Rows { get; } = [];
    public bool CanCreate => session.Can(PermissionCodes.AccountCreate);

    private List<AccountTreeNodeDto> _tree = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        var (cached, _) = await cache.GetAsync<List<AccountTreeNodeDto>>("coa_tree");
        if (cached is not null)
        {
            _tree = cached;
            ApplyFilter();
            IsOfflineData = true;
        }

        await RunAsync(async () =>
        {
            _tree = await api.GetAccountTreeAsync();
            await cache.SetAsync("coa_tree", _tree);
            IsOfflineData = false;
            ApplyFilter();
        }, useBusyFlag: cached is null);
    }

    partial void OnSearchChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Rows.Clear();
        var term = Search.Trim();
        foreach (var row in Flatten(_tree))
        {
            if (term.Length == 0
                || row.Node.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.Node.Code.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                Rows.Add(row);
            }
        }
    }

    private static IEnumerable<AccountRow> Flatten(IEnumerable<AccountTreeNodeDto> nodes)
    {
        foreach (var node in nodes)
        {
            yield return new AccountRow { Node = node };
            foreach (var child in Flatten(node.Children))
                yield return child;
        }
    }

    [RelayCommand]
    private async Task OpenAccountAsync(AccountRow? row)
    {
        if (row is null)
            return;
        if (!row.IsGroup)
            await Shell.Current.GoToAsync($"ledger?accountId={row.Node.Id}&accountName={Uri.EscapeDataString(row.Display)}");
    }

    [RelayCommand]
    private Task AddAccountAsync() => Shell.Current.GoToAsync("account-entry");
}
