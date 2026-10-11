using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class FamilySettingsPage : ContentPage
{
    private readonly FamilySettingsViewModel _viewModel;

    public FamilySettingsPage(FamilySettingsViewModel viewModel)
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
