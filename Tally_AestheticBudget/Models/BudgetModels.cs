using CommunityToolkit.Mvvm.ComponentModel;

namespace Tally_AestheticBudget.Models;

/// <summary>
/// One row in the Budget screen — represents one category's
/// spending, limit, and progress for a given month.
/// </summary>
public partial class BudgetCategoryItem : ObservableObject
{
    public int Id { get; set; }

    public ExpenseCategory Category { get; set; }

    // How much the user has set as their limit for this category
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    [NotifyPropertyChangedFor(nameof(IsOverLimit))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(LimitFormatted))]
    private decimal _limit;

    // How much has actually been spent this month in this category
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    [NotifyPropertyChangedFor(nameof(IsOverLimit))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(SpentFormatted))]
    private decimal _spent;

    // ── Computed display helpers ──────────────────────────────────────────────

    public string CategoryLabel => Category switch
    {
        ExpenseCategory.Food => "Food",
        ExpenseCategory.Transport => "Transport",
        ExpenseCategory.Shopping => "Shopping",
        ExpenseCategory.Health => "Health",
        ExpenseCategory.Fun => "Fun",
        ExpenseCategory.Grocery => "Grocery",
        _ => "Other"
    };

    public string CategoryIcon => Category switch
    {
        ExpenseCategory.Transport => "icon_transport.png",
        ExpenseCategory.Food => "icon_food.png",
        ExpenseCategory.Shopping => "icon_shopping.png",
        ExpenseCategory.Health => "icon_health.png",
        ExpenseCategory.Fun => "icon_fun.png",
        ExpenseCategory.Grocery => "icon_grocery.png",
        _ => "icon_default.png"
    };

    public string SpentFormatted => $"₱{Spent:N2}";
    public string LimitFormatted => Limit > 0 ? $"₱{Limit:N2}" : "No limit set";

    // Progress 0.0 → 1.0 capped at 1.0 for the bar width
    public double ProgressPercent =>
        Limit > 0 ? (double)Math.Min(Spent / Limit, 1.0m) : 0;

    // True when spending exceeds the limit — bar turns red
    public bool IsOverLimit => Limit > 0 && Spent > Limit;

    // e.g. "₱1,200 of ₱3,000" or "₱800 over limit"
    public string StatusLabel
    {
        get
        {
            if (Limit <= 0) return $"₱{Spent:N2} spent · no limit";
            if (IsOverLimit) return $"₱{Spent - Limit:N2} over limit";
            return $"₱{Spent:N2} of ₱{Limit:N2}";
        }
    }

    // ── Inline edit state ─────────────────────────────────────────────────────
    // These control the ✏️ inline edit row visibility

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditing))]
    private bool _isEditing;

    public bool IsNotEditing => !IsEditing;

    // The text the user types into the inline edit field
    [ObservableProperty]
    private string _editLimitText = string.Empty;
}