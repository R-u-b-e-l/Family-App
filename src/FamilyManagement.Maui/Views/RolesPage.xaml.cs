using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class RolesPage : ContentPage
{
    private readonly RolesViewModel _viewModel;

    public RolesPage(RolesViewModel viewModel)
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
