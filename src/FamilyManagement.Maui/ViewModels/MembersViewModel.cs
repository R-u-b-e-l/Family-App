using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class MembersViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private string _search = string.Empty;

    public ObservableCollection<UserDto> Members { get; } = [];
    public bool CanCreate => session.Can(PermissionCodes.MemberCreate);

    private CancellationTokenSource? _debounce;

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var result = await api.GetMembersAsync(search: string.IsNullOrWhiteSpace(Search) ? null : Search.Trim());
        Members.Clear();
        foreach (var member in result.Items)
            Members.Add(member);
    });

    partial void OnSearchChanged(string value)
    {
        _debounce?.Cancel();
        var cts = _debounce = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, cts.Token);
                await MainThread.InvokeOnMainThreadAsync(() => LoadCommand.ExecuteAsync(null));
            }
            catch (TaskCanceledException) { }
        });
    }

    [RelayCommand]
    private Task AddMemberAsync() => Shell.Current.GoToAsync("member-entry");

    public bool CanManage => session.Can(PermissionCodes.RoleManage);

    [RelayCommand]
    private async Task OpenStatementAsync(UserDto? member)
    {
        if (member is not null)
            await Shell.Current.GoToAsync($"member-statement?memberId={member.Id}");
    }

    [RelayCommand]
    private async Task ManageAsync(UserDto? member)
    {
        if (member is not null)
            await Shell.Current.GoToAsync($"member-manage?memberId={member.Id}");
    }
}

public partial class MemberEntryViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private RoleDto? _selectedRole;

    public ObservableCollection<RoleDto> Roles { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        if (Roles.Count > 0)
            return;
        var roles = await api.GetRolesAsync();
        foreach (var role in roles)
            Roles.Add(role);
        SelectedRole = Roles.FirstOrDefault(r => r.Name == "FamilyMember");
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            await ToastAsync("Name, username and password are required.");
            return;
        }

        await api.CreateMemberAsync(new CreateMemberRequest
        {
            FullName = FullName.Trim(),
            UserName = UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            Mobile = string.IsNullOrWhiteSpace(Mobile) ? null : Mobile.Trim(),
            Password = Password,
            RoleIds = SelectedRole is null ? [] : [SelectedRole.Id]
        });

        await ToastAsync("Member created");
        await Shell.Current.GoToAsync("..");
    });
}
