using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class RoleEditPage : ContentPage
{
    private readonly RoleEditViewModel _viewModel;

    public RoleEditPage(RoleEditViewModel viewModel)
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
