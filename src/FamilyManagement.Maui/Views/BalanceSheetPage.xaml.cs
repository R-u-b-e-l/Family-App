using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class BalanceSheetPage : ContentPage
{
    private readonly BalanceSheetViewModel _viewModel;

    public BalanceSheetPage(BalanceSheetViewModel viewModel)
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
