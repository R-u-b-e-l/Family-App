using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class VoucherDetailPage : ContentPage
{
    private readonly VoucherDetailViewModel _viewModel;

    public VoucherDetailPage(VoucherDetailViewModel viewModel)
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
