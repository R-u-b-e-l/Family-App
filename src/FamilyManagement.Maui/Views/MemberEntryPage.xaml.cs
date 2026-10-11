using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class MemberEntryPage : ContentPage
{
    private readonly MemberEntryViewModel _viewModel;

    public MemberEntryPage(MemberEntryViewModel viewModel)
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
