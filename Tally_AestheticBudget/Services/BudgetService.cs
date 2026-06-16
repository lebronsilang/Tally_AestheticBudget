using Tally_AestheticBudget.Models;
using static Tally_AestheticBudget.Models.BudgetCategoryItem;

namespace Tally_AestheticBudget.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IBudgetService
{
    // Per-category items for a single month (limit + spent), scale 1:1.
    // Kept for the Grocery screen and Wishlist affordability checks.
    Task<IEnumerable<BudgetCategoryItem>> GetBudgetItemsAsync(int year, int month);

    // Fully-computed page state for a given period (total, categories, unallocated).
    Task<BudgetOverview> GetOverviewAsync(BudgetPeriod period);

    // Persist a category limit / the monthly total. Both write to a specific month.
    Task SetLimitAsync(int year, int month, ExpenseCategory category, decimal limit);
    Task SetTotalAsync(int year, int month, decimal total);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class BudgetService : IBudgetService
{
    private readonly DatabaseService _db;
    private readonly IExpenseService _expenseService;
    private readonly ISettingsService _settings;

    // Sentinel Category value for the all-up monthly budget row.
    private const string TotalCategoryKey = "Total";

    // The categories we always surface on the Budget screen.
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
            Limit = limits.TryGetValue(cat, out var l) ? l : 0m,
            Spent = spent.TryGetValue(cat, out var s) ? s : 0m,
            CurrencySymbol = sym
        });
    }

    public async Task<BudgetOverview> GetOverviewAsync(BudgetPeriod period)
    {
        // 1. Spending for this window — reuse the proven expense queries.
        var spentItems = period.Mode switch
        {
            BudgetFilterMode.Day => await _expenseService.GetFeedItemsForDayAsync(period.Day),
            BudgetFilterMode.Week => await _expenseService.GetFeedItemsForWeekAsync(period.WeekMonday),
            BudgetFilterMode.Year => await _expenseService.GetFeedItemsForYearAsync(period.Year),
            _ => await _expenseService.GetFeedItemsForMonthAsync(period.Year, period.Month),
        };
        var spentByCat = SpentByCategory(spentItems);
        var totalSpent = spentItems.Sum(e => e.Amount);

        // 2. Limit source: a scaled single-month figure, or a year-wide sum.
        Dictionary<ExpenseCategory, decimal> limits;
        decimal total;
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
                limits = limits.ToDictionary(kv => kv.Key, kv => kv.Value * scale);
                total *= scale;
            }
            editable = period.Mode == BudgetFilterMode.Month;
        }

        // 3. Category rows.
        var sym = _settings.CurrencySymbol;
        var items = new List<BudgetCategoryItem>();
        decimal allocatedLimitSum = 0m;

        foreach (var cat in AllCategories)
        {
            var lim = limits.TryGetValue(cat, out var l) ? l : 0m;
            if (lim > 0) allocatedLimitSum += lim;

            items.Add(new BudgetCategoryItem
            {
                Category = cat,
                Limit = lim,
                Spent = spentByCat.TryGetValue(cat, out var s) ? s : 0m,
                CurrencySymbol = sym,
                CanEditLimit = editable
            });
        }

        // 4. Unallocated residual (opt-in): one summary row over the un-capped categories.
        if (_settings.AllotRemainingToUnallocated)
        {
            var uncapped = AllCategories
                .Where(c => !(limits.TryGetValue(c, out var l) && l > 0))
                .ToHashSet();

            items.Add(new BudgetCategoryItem
            {
                IsUnallocated = true,
                Limit = Math.Max(0m, total - allocatedLimitSum),
                Spent = spentByCat.Where(kv => uncapped.Contains(kv.Key)).Sum(kv => kv.Value),
                CurrencySymbol = sym,
                CanEditLimit = false
            });
        }

        return new BudgetOverview
        {
            TotalLimit = total,
            TotalSpent = totalSpent,
            IsEditable = editable,
            CurrencySymbol = sym,
            Items = items
        };
    }

    // ── Public writes ─────────────────────────────────────────────────────────

    public Task SetLimitAsync(int year, int month, ExpenseCategory category, decimal limit)
        => UpsertAsync(year, month, category.ToString(), limit);

    public Task SetTotalAsync(int year, int month, decimal total)
        => UpsertAsync(year, month, TotalCategoryKey, total);

    private async Task UpsertAsync(int year, int month, string categoryStr, decimal limit)
    {
        var db = await _db.GetConnectionAsync();
        var existing = await db.Table<BudgetEntity>()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync();
        var match = existing.FirstOrDefault(b =>
            string.Equals(b.Category, categoryStr, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            match.Limit = limit;
            await db.UpdateAsync(match);
        }
        else
        {
            await db.InsertAsync(new BudgetEntity
            {
                Category = categoryStr,
                Limit = limit,
                Month = month,
                Year = year
            });
        }
    }

    // ── Limit loading ───────────────────────────────────────────────────────────

    // Single month, with carry-forward. Splits out the "Total" sentinel.
    private async Task<(Dictionary<ExpenseCategory, decimal> limits, decimal total)>
        LoadMonthlyLimitsAsync(int year, int month)
    {
        var db = await _db.GetConnectionAsync();

        var saved = await db.Table<BudgetEntity>()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync();

        if (saved.Count == 0)
            saved = await CarryForwardLimitsAsync(year, month);

        var limits = new Dictionary<ExpenseCategory, decimal>();
        decimal total = 0m;

        foreach (var row in saved)
        {
            if (string.Equals(row.Category, TotalCategoryKey, StringComparison.OrdinalIgnoreCase))
                total = row.Limit;
            else if (Enum.TryParse<ExpenseCategory>(row.Category, true, out var cat))
                limits[cat] = row.Limit;
        }

        return (limits, total);
    }

    // Whole year: sum of stored monthly limits per category + total. No carry-forward —
    // months with no row contribute nothing, by design.
    private async Task<(Dictionary<ExpenseCategory, decimal> limits, decimal total)>
        LoadYearlyLimitsAsync(int year)
    {
        var db = await _db.GetConnectionAsync();
        var rows = await db.Table<BudgetEntity>().Where(b => b.Year == year).ToListAsync();

        var limits = new Dictionary<ExpenseCategory, decimal>();
        decimal total = 0m;

        foreach (var row in rows)
        {
            if (string.Equals(row.Category, TotalCategoryKey, StringComparison.OrdinalIgnoreCase))
            {
                total += row.Limit;
            }
            else if (Enum.TryParse<ExpenseCategory>(row.Category, true, out var cat))
            {
                limits[cat] = limits.TryGetValue(cat, out var existing)
                    ? existing + row.Limit
                    : row.Limit;
            }
        }

        return (limits, total);
    }

    // ── Carry forward ───────────────────────────────────────────────────────────
    // Copies the most recent prior month's rows (including the "Total" sentinel)
    // into the requested month when it has none of its own.

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