using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class AccountEntryPage : ContentPage
{
    private readonly AccountEntryViewModel _viewModel;

    public AccountEntryPage(AccountEntryViewModel viewModel)
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
