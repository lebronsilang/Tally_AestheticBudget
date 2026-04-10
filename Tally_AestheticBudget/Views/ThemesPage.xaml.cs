using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class ThemesPage : ContentPage
{
    public ThemesPage(ThemesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}