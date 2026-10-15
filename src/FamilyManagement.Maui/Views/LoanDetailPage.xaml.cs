using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class LoanDetailPage : ContentPage
{
    private readonly LoanDetailViewModel _viewModel;

    public LoanDetailPage(LoanDetailViewModel viewModel)
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
