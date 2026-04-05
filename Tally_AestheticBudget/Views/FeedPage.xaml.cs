using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class FeedPage : ContentPage
{
    // The ViewModel is injected automatically by MAUI's DI container
    // because we registered both FeedPage and FeedViewModel in MauiProgram.cs
    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}