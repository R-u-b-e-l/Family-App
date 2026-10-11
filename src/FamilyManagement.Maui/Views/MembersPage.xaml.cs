using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class MembersPage : ContentPage
{
    private readonly MembersViewModel _viewModel;

    public MembersPage(MembersViewModel viewModel)
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
