using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}