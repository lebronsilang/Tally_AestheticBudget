using Tally_AestheticBudget.Models;
using static Tally_AestheticBudget.Models.BudgetCategoryItem;

namespace Tally_AestheticBudget.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IBudgetService
{
    Task<IEnumerable<BudgetCategoryItem>> GetBudgetItemsAsync(int year, int month);

    Task<BudgetOverview> GetOverviewAsync(BudgetPeriod period);

    // null limit = Unlimited (no cap). 0 = an intentional ₱0 cap. > 0 = normal.
    Task SetLimitAsync(int year, int month, ExpenseCategory category, decimal? limit);
    Task SetTotalAsync(int year, int month, decimal total);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class BudgetService : IBudgetService
{
    private readonly DatabaseService _db;
    private readonly IExpenseService _expenseService;
    private readonly ISettingsService _settings;

    private const string TotalCategoryKey = "Total";

    // Stored value that encodes "Unlimited" in the non-nullable DB column.
    // Kept private — the sentinel never leaves this service; the domain uses decimal?.
    private const decimal UnlimitedSentinel = -1m;

    private static decimal? FromStored(decimal stored) => stored < 0m ? (decimal?)null : stored;
    private static decimal ToStored(decimal? limit) => limit ?? UnlimitedSentinel;

    private static readonly ExpenseCategory[] AllCategories =
    [
        ExpenseCategory.Food,
        ExpenseCategory.Transport,
        ExpenseCategory.Shopping,
        ExpenseCategory.Health,
        ExpenseCategory.Fun,
        ExpenseCategory.Grocery,
        ExpenseCategory.Other,
    ];

    public BudgetService(DatabaseService db, IExpenseService expenseService, ISettingsService settings)
    {
        _db = db;
        _expenseService = expenseService;
        _settings = settings;
    }

    // ── Public reads ────────────────────────────────────────────────────────────

    public async Task<IEnumerable<BudgetCategoryItem>> GetBudgetItemsAsync(int year, int month)
    {
        var (limits, _) = await LoadMonthlyLimitsAsync(year, month);
        var monthExpenses = await _expenseService.GetFeedItemsForMonthAsync(year, month);
        var spent = SpentByCategory(monthExpenses);
        var sym = _settings.CurrencySymbol;

        return AllCategories.Select(cat => new BudgetCategoryItem
        {
            Category = cat,
            Limit = limits.TryGetValue(cat, out var l) ? l : null,   // missing row → unlimited
            Spent = spent.TryGetValue(cat, out var s) ? s : 0m,
            CurrencySymbol = sym
        });
    }

    public async Task<BudgetOverview> GetOverviewAsync(BudgetPeriod period)
    {
        // 1. Spending for this window.
        var spentItems = period.Mode switch
        {
            BudgetFilterMode.Day => await _expenseService.GetFeedItemsForDayAsync(period.Day),
            BudgetFilterMode.Week => await _expenseService.GetFeedItemsForWeekAsync(period.WeekMonday),
            BudgetFilterMode.Year => await _expenseService.GetFeedItemsForYearAsync(period.Year),
            _ => await _expenseService.GetFeedItemsForMonthAsync(period.Year, period.Month),
        };
        var spentByCat = SpentByCategory(spentItems);
        var totalSpent = spentItems.Sum(e => e.Amount);

        // 2. Limits (nullable: null = unlimited).
        Dictionary<ExpenseCategory, decimal?> limits;
        decimal? total;
        bool editable;

        if (period.Mode == BudgetFilterMode.Year)
        {
            (limits, total) = await LoadYearlyLimitsAsync(period.Year);
            editable = false;
        }
        else
        {
            var (anchorYear, anchorMonth) = period.Mode switch
            {
                BudgetFilterMode.Day => (period.Day.Year, period.Day.Month),
                BudgetFilterMode.Week => (period.WeekMonday.Year, period.WeekMonday.Month),
                _ => (period.Year, period.Month),
            };

            (limits, total) = await LoadMonthlyLimitsAsync(anchorYear, anchorMonth);

            var daysInMonth = (decimal)DateTime.DaysInMonth(anchorYear, anchorMonth);
            var scale = period.Mode switch
            {
                BudgetFilterMode.Day => 1m / daysInMonth,
                BudgetFilterMode.Week => 7m / daysInMonth,
                _ => 1m,
            };

            if (scale != 1m)
            {
                // Scale numeric caps; unlimited stays unlimited.
                limits = limits.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value is decimal lv ? (decimal?)(lv * scale) : null);
                total = total is decimal tv ? (decimal?)(tv * scale) : null;
            }
            editable = period.Mode == BudgetFilterMode.Month;
        }

        // 3. Category rows.
        var sym = _settings.CurrencySymbol;
        var items = new List<BudgetCategoryItem>();
        decimal allocatedLimitSum = 0m;

        foreach (var cat in AllCategories)
        {
            var lim = limits.TryGetValue(cat, out var l) ? l : null; // missing → unlimited
            if (lim is decimal pos && pos > 0m) allocatedLimitSum += pos;

            items.Add(new BudgetCategoryItem
            {
                Category = cat,
                Limit = lim,
                Spent = spentByCat.TryGetValue(cat, out var s) ? s : 0m,
                CurrencySymbol = sym,
                CanEditLimit = editable
            });
        }

        // 4. Unallocated residual (opt-in): the un-capped (unlimited) categories.
        if (_settings.AllotRemainingToUnallocated)
        {
            var uncapped = AllCategories
                .Where(c => !(limits.TryGetValue(c, out var l) && l.HasValue))  // null/missing = uncapped
                .ToHashSet();

            items.Add(new BudgetCategoryItem
            {
                IsUnallocated = true,
                // If the total is itself unlimited, the residual is unlimited too.
                Limit = total is decimal t ? (decimal?)Math.Max(0m, t - allocatedLimitSum) : null,
                Spent = spentByCat.Where(kv => uncapped.Contains(kv.Key)).Sum(kv => kv.Value),
                CurrencySymbol = sym,
                CanEditLimit = false
            });
        }

        return new BudgetOverview
        {
            // For the *total* pill, "unlimited" and "unset" are the same UX, so we
            // collapse null → 0 here; HasTotal/IsOverTotal then behave as before.
            TotalLimit = total ?? 0m,
            TotalSpent = totalSpent,
            IsEditable = editable,
            CurrencySymbol = sym,
            Items = items
        };
    }

    // ── Public writes ─────────────────────────────────────────────────────────

    public Task SetLimitAsync(int year, int month, ExpenseCategory category, decimal? limit)
        => UpsertAsync(year, month, category.ToString(), ToStored(limit));

    public Task SetTotalAsync(int year, int month, decimal total)
        => UpsertAsync(year, month, TotalCategoryKey, total);

    private async Task UpsertAsync(int year, int month, string categoryStr, decimal storedLimit)
    {
        var db = await _db.GetConnectionAsync();
        var existing = await db.Table<BudgetEntity>()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync();
        var match = existing.FirstOrDefault(b =>
            string.Equals(b.Category, categoryStr, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            match.Limit = storedLimit;
            await db.UpdateAsync(match);
        }
        else
        {
            await db.InsertAsync(new BudgetEntity
            {
                Category = categoryStr,
                Limit = storedLimit,
                Month = month,
                Year = year
            });
        }
    }

    // ── Limit loading ───────────────────────────────────────────────────────────

    private async Task<(Dictionary<ExpenseCategory, decimal?> limits, decimal? total)>
        LoadMonthlyLimitsAsync(int year, int month)
    {
        var db = await _db.GetConnectionAsync();

        var saved = await db.Table<BudgetEntity>()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync();

        if (saved.Count == 0)
            saved = await CarryForwardLimitsAsync(year, month);

        var limits = new Dictionary<ExpenseCategory, decimal?>();
        decimal? total = null;

        foreach (var row in saved)
        {
            var val = FromStored(row.Limit);
            if (string.Equals(row.Category, TotalCategoryKey, StringComparison.OrdinalIgnoreCase))
                total = val;
            else if (Enum.TryParse<ExpenseCategory>(row.Category, true, out var cat))
                limits[cat] = val;
        }

        return (limits, total);
    }

    // Whole year: a category is unlimited for the year if ANY of its months is
    // unlimited (there is no finite ceiling); otherwise it's the sum of the caps.
    private async Task<(Dictionary<ExpenseCategory, decimal?> limits, decimal? total)>
        LoadYearlyLimitsAsync(int year)
    {
        var db = await _db.GetConnectionAsync();
        var rows = await db.Table<BudgetEntity>().Where(b => b.Year == year).ToListAsync();

        var sums = new Dictionary<ExpenseCategory, decimal>();
        var unlimited = new HashSet<ExpenseCategory>();
        var touched = new HashSet<ExpenseCategory>();

        decimal totalSum = 0m;
        bool totalTouched = false;
        bool totalUnlimited = false;

        foreach (var row in rows)
        {
            var val = FromStored(row.Limit);

            if (string.Equals(row.Category, TotalCategoryKey, StringComparison.OrdinalIgnoreCase))
            {
                totalTouched = true;
                if (val is decimal tv) totalSum += tv;
                else totalUnlimited = true;
            }
            else if (Enum.TryParse<ExpenseCategory>(row.Category, true, out var cat))
            {
                touched.Add(cat);
                if (val is decimal cv) sums[cat] = sums.GetValueOrDefault(cat) + cv;
                else unlimited.Add(cat);
            }
        }

        var limits = new Dictionary<ExpenseCategory, decimal?>();
        foreach (var cat in touched)
            limits[cat] = unlimited.Contains(cat) ? null : sums.GetValueOrDefault(cat);

        decimal? total = totalTouched ? (totalUnlimited ? null : totalSum) : null;

        return (limits, total);
    }

    // ── Carry forward ───────────────────────────────────────────────────────────
    // Copies the most recent prior month's rows (the sentinel -1 carries verbatim,
    // so an unlimited category stays unlimited next month).

    private async Task<List<BudgetEntity>> CarryForwardLimitsAsync(int year, int month)
    {
        var db = await _db.GetConnectionAsync();

        var allPrevious = await db.Table<BudgetEntity>()
            .Where(b => b.Year < year || (b.Year == year && b.Month < month))
            .ToListAsync();

        if (allPrevious.Count == 0) return [];

        var latest = allPrevious
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .First();

        var previousLimits = allPrevious
            .Where(b => b.Year == latest.Year && b.Month == latest.Month)
            .ToList();

        var carried = new List<BudgetEntity>();
        foreach (var prev in previousLimits)
        {
            var entity = new BudgetEntity
            {
                Category = prev.Category,
                Limit = prev.Limit,
                Month = month,
                Year = year
            };
            await db.InsertAsync(entity);
            carried.Add(entity);
        }

        return carried;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<ExpenseCategory, decimal> SpentByCategory(
        IEnumerable<FeedCardItem> items) =>
        items.GroupBy(e => e.Category)
             .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
}