using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class LoansPage : ContentPage
{
    private readonly LoansViewModel _viewModel;

    public LoansPage(LoansViewModel viewModel)
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
