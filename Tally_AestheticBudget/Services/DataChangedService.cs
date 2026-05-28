namespace Tally_AestheticBudget.Services;

/// <summary>
/// Lightweight pub/sub service so ViewModels can signal each other
/// when data has changed (e.g. Settings clears expenses → Feed needs reload,
/// Grocery "Buy Checked" → Feed needs reload, etc.)
/// Registered as a singleton in DI.
/// </summary>
public class DataChangedService
{
    /// <summary>Fires when expense/feed data has been modified.</summary>
    public event Action? ExpensesChanged;

    /// <summary>Fires when grocery list data has been modified.</summary>
    public event Action? GroceryChanged;

    /// <summary>Fires when wishlist data has been modified.</summary>
    public event Action? WishlistChanged;

    /// <summary>Fires when budget data may need refresh (e.g. expenses changed).</summary>
    public event Action? BudgetChanged;

    public void NotifyExpensesChanged()
    {
        ExpensesChanged?.Invoke();
        BudgetChanged?.Invoke(); // expenses affect budget spent totals
    }

    public void NotifyGroceryChanged()
    {
        GroceryChanged?.Invoke();
        ExpensesChanged?.Invoke(); // "Buy Checked" creates expense rows
        BudgetChanged?.Invoke();
    }

    public void NotifyWishlistChanged() => WishlistChanged?.Invoke();

    /// <summary>Fires all changed events — used by "Clear All" in Settings.</summary>
    public void NotifyAllChanged()
    {
        ExpensesChanged?.Invoke();
        GroceryChanged?.Invoke();
        WishlistChanged?.Invoke();
        BudgetChanged?.Invoke();
    }
}