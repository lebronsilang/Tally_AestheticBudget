using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class GroceryPage : ContentPage
{
    private readonly GroceryViewModel _viewModel;

    public GroceryPage(GroceryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnPageAppearingAsync();
    }
}