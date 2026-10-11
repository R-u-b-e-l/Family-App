using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class ProfitLossPage : ContentPage
{
    private readonly ProfitLossViewModel _viewModel;

    public ProfitLossPage(ProfitLossViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.GenerateCommand.ExecuteAsync(null);
    }
}
