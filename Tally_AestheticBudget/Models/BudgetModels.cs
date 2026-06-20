using CommunityToolkit.Mvvm.ComponentModel;
using Tally_AestheticBudget.Helpers;

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

    /// <summary>
    /// Phosphor glyph for this category (replaces the old PNG-based CategoryIcon).
    /// Bind to a Label with FontFamily="PhosphorIcons".
    /// </summary>
    public string CategoryGlyph => IsUnallocated ? PhosphorIcons.Default : Category switch
    {
        ExpenseCategory.Transport => PhosphorIcons.Transport,
        ExpenseCategory.Food => PhosphorIcons.Food,
        ExpenseCategory.Shopping => PhosphorIcons.Shopping,
        ExpenseCategory.Health => PhosphorIcons.Health,
        ExpenseCategory.Fun => PhosphorIcons.Fun,
        ExpenseCategory.Grocery => PhosphorIcons.Grocery,
        _ => PhosphorIcons.Default
    };

    // DEPRECATED — retained only so any stray binding keeps compiling.
    // Remove once every XAML site has migrated to CategoryGlyph.
    public string CategoryIcon => IsUnallocated ? "icon_default.png" : Category switch
    {
        ExpenseCategory.Transport => "icon_transport.png",
        ExpenseCategory.Food => "icon_food.png",
        ExpenseCategory.Shopping => "icon_shopping.png",
        ExpenseCategory.Health => "icon_health.png",
        ExpenseCategory.Fun => "icon_fun.png",
        ExpenseCategory.Grocery => "icon_grocery.png",
        _ => "icon_default.png"
    };

    // Set by BudgetService when building this item — avoids hardcoding ₱
    public string CurrencySymbol { get; set; } = "₱";

    public string SpentFormatted => $"{CurrencySymbol}{Spent:N2}";
    public string LimitFormatted => Limit > 0 ? $"{CurrencySymbol}{Limit:N2}" : "No limit set";

    // e.g. "₱1,200 of ₱3,000" or "₱800 over limit"
    public string StatusLabel
    {
        get
        {
            if (Limit <= 0) return $"{CurrencySymbol}{Spent:N2} spent · no limit";
            if (IsOverLimit) return $"{CurrencySymbol}{Spent - Limit:N2} over limit";
            return $"{CurrencySymbol}{Spent:N2} of {CurrencySymbol}{Limit:N2}";
        }
    }

    // Progress 0.0 -> 1.0 capped at 1.0 for the bar width
    public double ProgressPercent =>
        Limit > 0 ? (double)Math.Min(Spent / Limit, 1.0m) : 0;

    // True when spending exceeds the limit then bar turns red
    public bool IsOverLimit => Limit > 0 && Spent > Limit;

    // ── Inline edit state ─────────────────────────────────────────────────────
    // These control the inline edit row visibility

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditing))]
    [NotifyPropertyChangedFor(nameof(ShowEditButton))]
    private bool _isEditing;

    public bool IsNotEditing => !IsEditing;

    // ── Unallocated / period state ─────────────────────────────────────────────

    // The residual "Unallocated" summary row — derived, never directly editable.
    public bool IsUnallocated { get; set; }

    // False in This Year view (read-only aggregate) and on the Unallocated row.
    public bool CanEditLimit { get; set; } = true;

    public bool ShowEditButton => CanEditLimit && IsNotEditing;
    public string DisplayName => IsUnallocated ? "Unallocated" : CategoryLabel;

    // The text the user types into the inline edit field
    [ObservableProperty]
    private string _editLimitText = string.Empty;

    /// <summary>Re-raises accent-dependent computed properties after a theme change.</summary>
    public void RefreshThemeBindings() => OnPropertyChanged(nameof(IsOverLimit));

    /// <summary>How the Budget screen scales limits and scopes spending.</summary>
    public enum BudgetFilterMode { Day, Week, Month, Year }

    /// <summary>
    /// Describes the window the Budget page is showing. Only the fields relevant to
    /// <see cref="Mode"/> are read; the view-model fills the rest with harmless defaults.
    /// </summary>
    public sealed record BudgetPeriod(
        BudgetFilterMode Mode,
        DateTime Day,
        DateTime WeekMonday,
        int Year,
        int Month);

    /// <summary>Everything the Budget page renders for one period, fully computed by the service.</summary>
    public sealed class BudgetOverview
    {
        public decimal TotalLimit { get; init; }
        public decimal TotalSpent { get; init; }
        public bool IsEditable { get; init; }
        public string CurrencySymbol { get; init; } = "₱";
        public List<BudgetCategoryItem> Items { get; init; } = [];

        public bool HasTotal => TotalLimit > 0;
        public decimal TotalRemaining => TotalLimit - TotalSpent;
        public bool IsOverTotal => TotalLimit > 0 && TotalSpent > TotalLimit;
    }
}