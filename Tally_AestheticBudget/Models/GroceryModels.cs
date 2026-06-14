using CommunityToolkit.Mvvm.ComponentModel;

namespace Tally_AestheticBudget.Models;

/// <summary>
/// Represents one item in the pending grocery list.
/// </summary>
public partial class GroceryItem : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceFormatted))]
    [NotifyPropertyChangedFor(nameof(TotalFormatted))]
    private decimal _price;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFormatted))]
    private int _quantity = 1;

    [ObservableProperty]
    private bool _isChecked;

    public DateTime CreatedAt { get; set; }

    // ── Display helpers ───────────────────────────────────────────────────────

    public string CurrencySymbol { get; set; } = "₱";
    public string PriceFormatted => Price > 0 ? $"{CurrencySymbol}{Price:N2}" : $"{CurrencySymbol}0.00";
    public string TotalFormatted => $"{CurrencySymbol}{Price * Quantity:N2}";

    public string QuantityLabel => Quantity > 1 ? $"qty: {Quantity}" : string.Empty;
}

/// <summary>
/// Represents the grocery budget status bar at the top of the screen.
/// Shows how much of the Grocery budget has been spent vs remaining.
/// </summary>
public partial class GroceryBudgetStatus : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(RemainingLabel))]
    private decimal _budgetLimit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(RemainingLabel))]
    private decimal _alreadySpent;

    public void RefreshThemeBindings() => OnPropertyChanged(nameof(IsOverBudget));

    // Total of checked items (pending purchase) — updates live as user checks items
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(RemainingLabel))]
    private decimal _pendingTotal;

    public bool HasBudget => BudgetLimit > 0;

    public double ProgressPercent =>
        BudgetLimit > 0
            ? (double)Math.Min((AlreadySpent + PendingTotal) / BudgetLimit, 1.0m)
            : 0;

    public bool IsOverBudget =>
        BudgetLimit > 0 && (AlreadySpent + PendingTotal) > BudgetLimit;

    public string CurrencySymbol { get; set; } = "₱";

    public string StatusLabel =>
        HasBudget
            ? $"Grocery Budget · {CurrencySymbol}{AlreadySpent + PendingTotal:N2}"
            : "No grocery budget set — go to Budget to add one";

    public string RemainingLabel
    {
        get
        {
            if (!HasBudget) return string.Empty;
            var remaining = BudgetLimit - AlreadySpent - PendingTotal;
            return remaining >= 0
                ? $"{CurrencySymbol}{remaining:N2} left"
                : $"Over by {CurrencySymbol}{Math.Abs(remaining):N2}";
        }
    }
}

public enum GroceryFilter { All, Pending, Checked }