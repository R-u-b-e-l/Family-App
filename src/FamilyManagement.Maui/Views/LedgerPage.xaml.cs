using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class LedgerPage : ContentPage
{
    private readonly LedgerViewModel _viewModel;

    public LedgerPage(LedgerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitCommand.ExecuteAsync(null);
    }
}
