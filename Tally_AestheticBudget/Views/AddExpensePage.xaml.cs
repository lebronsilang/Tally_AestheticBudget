using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class AddExpensePage : ContentPage
{
    private readonly AddExpenseViewModel _viewModel;

    public AddExpensePage(AddExpenseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // In edit mode this pre-fills the form with the existing expense data.
        // In add mode this does nothing (IsEditMode = false).
        await _viewModel.LoadExistingAsync();
    }
}