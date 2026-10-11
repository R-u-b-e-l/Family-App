using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class MorePage : ContentPage
{
    private readonly MoreViewModel _viewModel;

    public MorePage(MoreViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}
