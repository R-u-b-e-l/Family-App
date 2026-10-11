using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class VouchersPage : ContentPage
{
    private readonly VouchersViewModel _viewModel;

    public VouchersPage(VouchersViewModel viewModel)
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
