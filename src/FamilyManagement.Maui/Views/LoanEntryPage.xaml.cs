using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class LoanEntryPage : ContentPage
{
    private readonly LoanEntryViewModel _viewModel;

    public LoanEntryPage(LoanEntryViewModel viewModel)
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
