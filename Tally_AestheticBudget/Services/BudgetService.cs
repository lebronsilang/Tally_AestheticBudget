using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IBudgetService
{
    // Load all category items for a given month, with spent amounts calculated
    Task<IEnumerable<BudgetCategoryItem>> GetBudgetItemsAsync(int year, int month);

    // Save or update a category's limit for a given month
    Task SetLimitAsync(int year, int month, ExpenseCategory category, decimal limit);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class BudgetService : IBudgetService
{
    private readonly DatabaseService _db;
    private readonly IExpenseService _expenseService;

    // The 6 categories we always show on the Budget screen
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

    public BudgetService(DatabaseService db, IExpenseService expenseService)
    {
        _db = db;
        _expenseService = expenseService;
    }

    public async Task<IEnumerable<BudgetCategoryItem>> GetBudgetItemsAsync(int year, int month)
    {
        var db = await _db.GetConnectionAsync();

        // Load saved limits for this month
        var savedLimits = await db.Table<BudgetEntity>()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync();

        // If no limits exist for this month, carry forward from previous month
        if (savedLimits.Count == 0)
            savedLimits = await CarryForwardLimitsAsync(year, month);

        // Load all expenses for this month to calculate spent amounts
        var monthExpenses = await _expenseService.GetFeedItemsForMonthAsync(year, month);
        var spentByCategory = monthExpenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        // Build one BudgetCategoryItem per category
        var items = AllCategories.Select(cat =>
        {
            var saved = savedLimits.FirstOrDefault(b =>
                string.Equals(b.Category, cat.ToString(), StringComparison.OrdinalIgnoreCase));

            return new BudgetCategoryItem
            {
                Id = saved?.Id ?? 0,
                Category = cat,
                Limit = saved?.Limit ?? 0,
                Spent = spentByCategory.TryGetValue(cat, out var s) ? s : 0
            };
        });

        return items;
    }

    public async Task SetLimitAsync(int year, int month, ExpenseCategory category, decimal limit)
    {
        var db = await _db.GetConnectionAsync();

        var categoryStr = category.ToString();
        var existing = await db.Table<BudgetEntity>()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync();
        var match = existing.FirstOrDefault(b => b.Category == categoryStr);

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

    // ── Carry forward ─────────────────────────────────────────────────────────
    // If no budgets exist for the requested month, copy limits from the
    // most recent previous month that has data. Returns empty list if none found.

    private async Task<List<BudgetEntity>> CarryForwardLimitsAsync(int year, int month)
    {
        var db = await _db.GetConnectionAsync();

        // Find all budget rows from before this month
        var allPrevious = await db.Table<BudgetEntity>()
            .Where(b => b.Year < year || (b.Year == year && b.Month < month))
            .ToListAsync();

        if (allPrevious.Count == 0) return [];

        // Find the most recent month that has budget data
        var latestEntry = allPrevious
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .First();

        var latestYear = latestEntry.Year;
        var latestMonth = latestEntry.Month;

        // Get all limits from that month and copy them into the new month
        var previousLimits = allPrevious
            .Where(b => b.Year == latestYear && b.Month == latestMonth)
            .ToList();

        var carried = new List<BudgetEntity>();

        foreach (var prev in previousLimits)
        {
            var newEntity = new BudgetEntity
            {
                Category = prev.Category,
                Limit = prev.Limit,
                Month = month,
                Year = year
            };
            await db.InsertAsync(newEntity);
            carried.Add(newEntity);
        }

        return carried;
    }
}