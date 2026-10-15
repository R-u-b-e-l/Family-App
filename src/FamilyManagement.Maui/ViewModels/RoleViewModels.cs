using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class RolesViewModel(ApiClient api) : BaseViewModel
{
    public ObservableCollection<RoleDto> Roles { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var roles = await api.GetRolesAsync();
        Roles.Clear();
        foreach (var role in roles)
            Roles.Add(role);
    });

    [RelayCommand]
    private async Task OpenRoleAsync(RoleDto? role)
    {
        if (role is not null)
            await Shell.Current.GoToAsync($"role-edit?roleId={role.Id}");
    }

    [RelayCommand]
    private Task NewRoleAsync() => Shell.Current.GoToAsync("role-edit");
}

public partial class PermissionItemVm : ObservableObject
{
    public string Code { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Display => Code.Contains('.') ? Code[(Code.IndexOf('.') + 1)..] : Code;
    [ObservableProperty] private bool _isChecked;
    public bool IsFirstOfGroup { get; init; }
}

[QueryProperty(nameof(RoleIdParam), "roleId")]
public partial class RoleEditViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private string _roleIdParam = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditPermissions))]
    private bool _isAdminRole;
    [ObservableProperty] private bool _isSystemRole;

    public ObservableCollection<PermissionItemVm> Permissions { get; } = [];
    public bool CanEditPermissions => !IsAdminRole;
    private bool IsNew => !Guid.TryParse(RoleIdParam, out _);

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var catalog = await api.GetPermissionCatalogAsync();

        RoleDto? role = null;
        if (Guid.TryParse(RoleIdParam, out var roleId))
        {
            var roles = await api.GetRolesAsync();
            role = roles.FirstOrDefault(r => r.Id == roleId);
        }

        Name = role?.Name ?? "";
        Description = role?.Description ?? "";
        IsSystemRole = role?.IsSystem == true;
        IsAdminRole = role?.Name == "FamilyAdmin";
        Title = role is null ? "নতুন Role" : role.Name;

        Permissions.Clear();
        string? lastGroup = null;
        foreach (var permission in catalog)
        {
            Permissions.Add(new PermissionItemVm
            {
                Code = permission.Code,
                Group = permission.Group,
                IsChecked = role?.Permissions.Contains(permission.Code) == true,
                IsFirstOfGroup = permission.Group != lastGroup
            });
            lastGroup = permission.Group;
        }
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        if (IsAdminRole)
        {
            await ToastAsync("FamilyAdmin সবসময় সব permission রাখে — বদলানো যায় না।");
            return;
        }

        var selectedCodes = Permissions.Where(p => p.IsChecked).Select(p => p.Code).ToList();

        if (IsNew)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await ToastAsync("Role-এর নাম লিখুন।");
                return;
            }
            await api.CreateRoleAsync(new CreateRoleRequest
            {
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                PermissionCodes = selectedCodes
            });
        }
        else
        {
            var roleId = Guid.Parse(RoleIdParam);
            if (!IsSystemRole)
            {
                await api.UpdateRoleAsync(roleId, new UpdateRoleRequest
                {
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
                });
            }
            await api.UpdateRolePermissionsAsync(roleId, new UpdateRolePermissionsRequest
            {
                PermissionCodes = selectedCodes
            });
        }

        await ToastAsync("Role সেভ হয়েছে ✓");
        await Shell.Current.GoToAsync("..");
    });
}

[QueryProperty(nameof(MemberIdParam), "memberId")]
public partial class MemberManageViewModel(ApiClient api, Session session) : BaseViewModel
{
    public partial class RoleCheckVm : ObservableObject
    {
        public required RoleDto Role { get; init; }
        [ObservableProperty] private bool _isChecked;
    }

    [ObservableProperty] private string _memberIdParam = string.Empty;
    [ObservableProperty] private UserDto? _member;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _newPassword = string.Empty;

    public ObservableCollection<RoleCheckVm> Roles { get; } = [];

    private Guid MemberId => Guid.TryParse(MemberIdParam, out var id) ? id : Guid.Empty;

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        Member = await api.GetMemberAsync(MemberId);
        IsActive = Member.Status == Shared.Enums.EntityStatus.Active;
        Title = Member.FullName;

        var roles = await api.GetRolesAsync();
        Roles.Clear();
        foreach (var role in roles)
            Roles.Add(new RoleCheckVm { Role = role, IsChecked = Member.Roles.Contains(role.Name) });
    });

    [RelayCommand]
    private Task SaveRolesAsync() => RunAsync(async () =>
    {
        var selected = Roles.Where(r => r.IsChecked).Select(r => r.Role.Id).ToList();
        if (selected.Count == 0)
        {
            await ToastAsync("অন্তত একটা role দিতে হবে।");
            return;
        }
        await api.AssignRolesAsync(MemberId, new AssignRolesRequest { RoleIds = selected });
        await ToastAsync("Role আপডেট হয়েছে ✓");
    });

    [RelayCommand]
    private Task SaveStatusAsync() => RunAsync(async () =>
    {
        await api.SetMemberStatusAsync(MemberId, new UpdateMemberStatusRequest
        {
            Status = IsActive ? Shared.Enums.EntityStatus.Active : Shared.Enums.EntityStatus.Inactive
        });
        await ToastAsync(IsActive ? "Member সক্রিয় করা হয়েছে" : "Member নিষ্ক্রিয় করা হয়েছে");
    });

    [RelayCommand]
    private Task ResetPasswordAsync() => RunAsync(async () =>
    {
        if (NewPassword.Length < 8)
        {
            await ToastAsync("Password কমপক্ষে ৮ অক্ষরের হতে হবে (১টা সংখ্যা সহ)।");
            return;
        }
        await api.AdminResetPasswordAsync(MemberId, new AdminResetPasswordRequest { NewPassword = NewPassword });
        NewPassword = string.Empty;
        await ToastAsync("Password reset হয়েছে ✓");
    });
}
