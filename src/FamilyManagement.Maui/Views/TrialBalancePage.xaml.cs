using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class TrialBalancePage : ContentPage
{
    private readonly TrialBalanceViewModel _viewModel;

    public TrialBalancePage(TrialBalanceViewModel viewModel)
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
