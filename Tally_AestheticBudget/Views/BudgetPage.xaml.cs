using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class BudgetPage : ContentPage
{
    private readonly BudgetViewModel _viewModel;

    public BudgetPage(BudgetViewModel viewModel)
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
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is ViewModels.BudgetViewModel vm)
            vm.OnPageDisappearing();
    }
}