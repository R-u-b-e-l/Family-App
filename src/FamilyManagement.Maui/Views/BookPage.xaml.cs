using FamilyManagement.Maui.ViewModels;

namespace FamilyManagement.Maui.Views;

public partial class BookPage : ContentPage
{
    private readonly BookViewModel _viewModel;

    public BookPage(BookViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.GenerateCommand.ExecuteAsync(null);
    }
}
