using CommunityToolkit.Mvvm.ComponentModel;

namespace Tally_AestheticBudget.Models;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum WishPriority { Want, Need, Someday }
public enum WishStatus { Planned, Bought }
public enum WishFilter { All, Planned, Bought, Want, Need, Someday }

// ── Wish card item ────────────────────────────────────────────────────────────

public partial class WishCardItem : ObservableObject
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public WishPriority Priority { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? Caption { get; set; }
    public string? PhotoPath { get; set; }
    public string? TargetMonth { get; set; }
    public DateTime CreatedAt { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsBought))]
    private WishStatus _status;

    [ObservableProperty]
    private string? _regretRating;

    [ObservableProperty]
    private bool _isPinned;

    // ── Computed display helpers ──────────────────────────────────────────────

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    public bool HasCaption => !string.IsNullOrEmpty(Caption);
    public bool IsBought => Status == WishStatus.Bought;

    public string PriceFormatted => $"₱{Price:N2}";

    public string PriorityLabel => Priority switch
    {
        WishPriority.Need => "🔥 Need",
        WishPriority.Someday => "🌙 Someday",
        _ => "💖 Want"
    };

    public string PriorityEmoji => Priority switch
    {
        WishPriority.Need => "🔥",
        WishPriority.Someday => "🌙",
        _ => "💖"
    };

    public string StatusLabel => Status == WishStatus.Bought ? "✓ Bought" : "Planned";

    public string CategoryLabel => Category switch
    {
        ExpenseCategory.Food => "Food",
        ExpenseCategory.Transport => "Transport",
        ExpenseCategory.Shopping => "Shopping",
        ExpenseCategory.Health => "Health",
        ExpenseCategory.Fun => "Fun",
        _ => "Other"
    };

    public string TargetMonthFormatted
    {
        get
        {
            if (string.IsNullOrEmpty(TargetMonth)) return string.Empty;
            var parts = TargetMonth.Split('-');
            if (parts.Length != 2) return TargetMonth;
            if (int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var m))
                return new DateTime(y, m, 1).ToString("MMMM yyyy");
            return TargetMonth;
        }
    }

    // ── Cooling-off & stale detection ─────────────────────────────────────────

    private int DaysSinceAdded => (DateTime.Today - CreatedAt.Date).Days;

    public bool IsInCoolingOff => DaysSinceAdded < 3;
    public bool IsStale => !IsInCoolingOff && DaysSinceAdded >= 7 && Status == WishStatus.Planned;

    public string CoolingOffLabel =>
        IsInCoolingOff ? $"⏳ {3 - DaysSinceAdded}d cooling-off left" : string.Empty;

    public string StaleLabel =>
        IsStale ? $"👀 Added {DaysSinceAdded}d ago — still want it?" : string.Empty;

    public bool ShowCoolingBanner => IsInCoolingOff && Status == WishStatus.Planned;
    public bool ShowStaleBanner => IsStale;

    // ── Regret display ────────────────────────────────────────────────────────

    public bool HasRegretRating => !string.IsNullOrEmpty(RegretRating);
    public bool IsWorthIt => RegretRating == "Worth";
    public string RegretLabel => RegretRating == "Worth" ? "😌 Worth it" : "😭 Regret";
}

// ── Afford result ─────────────────────────────────────────────────────────────

public class AffordResult
{
    public bool CanAfford { get; set; }
    public decimal BudgetRemaining { get; set; }
    public decimal Difference { get; set; }

    public string Label => CanAfford
        ? $"✓ You can afford this — ₱{BudgetRemaining:N2} remaining in {CategoryName}"
        : $"✗ ₱{Math.Abs(Difference):N2} over your {CategoryName} budget";

    public string CategoryName { get; set; } = string.Empty;
}