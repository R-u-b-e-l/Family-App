using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class MemberManagePage : ContentPage
{
    private readonly MemberManageViewModel _viewModel;

    public MemberManagePage(MemberManageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
