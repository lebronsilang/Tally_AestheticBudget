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

    // The user's limit for this category.
    //   null  → Unlimited (no cap, no over-warning, neutral/hidden progress)
    //   0     → an intentional ₱0 budget (any spend counts as over)
    //   > 0   → normal cap
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressPercent))]
    [NotifyPropertyChangedFor(nameof(IsOverLimit))]
    [NotifyPropertyChangedFor(nameof(IsUnlimited))]
    [NotifyPropertyChangedFor(nameof(HasLimit))]
    [NotifyPropertyChangedFor(nameof(ShowProgressBar))]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(LimitFormatted))]
    private decimal? _limit;

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

    // ── Limit-state helpers (item 5) ──────────────────────────────────────────

    /// <summary>No cap at all — spending is never flagged.</summary>
    public bool IsUnlimited => Limit is null;

    /// <summary>A cap exists (including an intentional ₱0).</summary>
    public bool HasLimit => Limit is not null;

    /// <summary>
    /// A determinate progress fill only makes sense for a positive cap. Bind the
    /// whole progress track's IsVisible to <see cref="HasLimit"/> (hide for
    /// unlimited); a ₱0 cap still shows a determinate track that fills red the
    /// moment anything is spent.
    /// </summary>
    public bool ShowProgressBar => Limit is decimal v && v > 0m;

    public string LimitFormatted => Limit is decimal v
        ? $"{CurrencySymbol}{v:N2}"
        : "Unlimited";

    // e.g. "₱1,200 of ₱3,000", "₱800 over limit", "₱1,200 spent · unlimited"
    public string StatusLabel
    {
        get
        {
            if (Limit is not decimal lim)
                return $"{CurrencySymbol}{Spent:N2} spent · unlimited";
            if (Spent > lim)
                return $"{CurrencySymbol}{Spent - lim:N2} over limit";
            if (lim == 0m)
                return $"{CurrencySymbol}0.00 budget";
            return $"{CurrencySymbol}{Spent:N2} of {CurrencySymbol}{lim:N2}";
        }
    }

    // Progress 0.0 -> 1.0 for the bar width.
    public double ProgressPercent
    {
        get
        {
            if (Limit is not decimal lim) return 0d;        // unlimited → track hidden anyway
            if (lim <= 0m) return Spent > 0m ? 1d : 0d;     // ₱0 cap → full once anything is spent
            return (double)Math.Min(Spent / lim, 1.0m);
        }
    }

    // True when spending exceeds the cap. For a ₱0 cap, any spend is "over".
    public bool IsOverLimit => Limit is decimal lim && Spent > lim;

    // ── Inline edit state ─────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditing))]
    [NotifyPropertyChangedFor(nameof(ShowEditButton))]
    private bool _isEditing;

    public bool IsNotEditing => !IsEditing;

    /// <summary>Edit-row toggle: when on, Save persists a null (unlimited) limit.</summary>
    [ObservableProperty]
    private bool _editIsUnlimited;

    // ── Unallocated / period state ─────────────────────────────────────────────

    public bool IsUnallocated { get; set; }

    public bool CanEditLimit { get; set; } = true;

    public bool ShowEditButton => CanEditLimit && IsNotEditing;
    public string DisplayName => IsUnallocated ? "Unallocated" : CategoryLabel;

    [ObservableProperty]
    private string _editLimitText = string.Empty;

    /// <summary>Re-raises accent-dependent computed properties after a theme change.</summary>
    public void RefreshThemeBindings() => OnPropertyChanged(nameof(IsOverLimit));

    /// <summary>How the Budget screen scales limits and scopes spending.</summary>
    public enum BudgetFilterMode { Day, Week, Month, Year }

    public sealed record BudgetPeriod(
        BudgetFilterMode Mode,
        DateTime Day,
        DateTime WeekMonday,
        int Year,
        int Month);

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