using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class AddExpensePage : ContentPage
{
    public AddExpensePage(AddExpenseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}