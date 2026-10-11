using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class VoucherEntryPage : ContentPage
{
    private readonly VoucherEntryViewModel _viewModel;

    public VoucherEntryPage(VoucherEntryViewModel viewModel)
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
