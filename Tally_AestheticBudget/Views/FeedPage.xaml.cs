using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class FeedPage : ContentPage
{
    private readonly FeedViewModel _viewModel;

    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    // Fires every time the Feed screen becomes visible —
    // including when returning from AddExpensePage after saving.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnPageAppearingAsync();
    }
}