using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class MyDashboardPage : ContentPage
{
    private readonly MyDashboardViewModel _viewModel;

    public MyDashboardPage(MyDashboardViewModel viewModel)
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
