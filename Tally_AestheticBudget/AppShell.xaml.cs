using Tally_AestheticBudget.Views;

namespace Tally_AestheticBudget;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(AddExpensePage), typeof(AddExpensePage));
    }
}