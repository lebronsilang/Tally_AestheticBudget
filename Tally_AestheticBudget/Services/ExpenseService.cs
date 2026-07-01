using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

public interface IExpenseService
{
    Task<IEnumerable<FeedCardItem>> GetAllFeedItemsAsync();
    Task<IEnumerable<FeedCardItem>> GetFeedItemsForYearAsync(int year);
    Task<IEnumerable<FeedCardItem>> GetFeedItemsForMonthAsync(int year, int month);
    Task<IEnumerable<FeedCardItem>> GetFeedItemsForDayAsync(DateTime date);
    Task<IEnumerable<FeedCardItem>> GetFeedItemsForWeekAsync(DateTime weekStart);
    Task<IEnumerable<FeedCardItem>> GetFeedItemsByCategoryAsync(ExpenseCategory category);
    Task<ExpenseEntity?> GetExpenseByIdAsync(int id);
    Task SaveExpenseAsync(ExpenseEntity expense);
    Task UpdateExpenseAsync(ExpenseEntity expense);
    Task DeleteExpenseAsync(int id);
    Task DeleteGroceryGroupAsync(int groupId);
    Task DeleteGroceryLineItemAsync(int lineItemId);
    Task DeleteAllAsync();
    Task<List<int>> GetDistinctExpenseYearsAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly DatabaseService _db;
    private readonly ISettingsService _settings;

    public ExpenseService(DatabaseService db, ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public Task<IEnumerable<FeedCardItem>> GetAllFeedItemsAsync()
        => QueryAsync(null, null);

    public Task<IEnumerable<FeedCardItem>> GetFeedItemsForYearAsync(int year)
        => QueryAsync(year, null);

    public Task<IEnumerable<FeedCardItem>> GetFeedItemsForMonthAsync(int year, int month)
        => QueryAsync(year, month);

    public Task<IEnumerable<FeedCardItem>> GetFeedItemsForDayAsync(DateTime date)
        => QueryAsync(date.Year, date.Month, date.Day);

    public Task<IEnumerable<FeedCardItem>> GetFeedItemsForWeekAsync(DateTime weekStart)
        => QueryAsync(null, null, null, weekStart, weekStart.AddDays(7));

    public Task<IEnumerable<FeedCardItem>> GetFeedItemsByCategoryAsync(ExpenseCategory category)
        => QueryAsync(null, null, null, null, null, category);

    private async Task<IEnumerable<FeedCardItem>> QueryAsync(
        int? year, int? month, int? day = null,
        DateTime? rangeStart = null, DateTime? rangeEnd = null,
        ExpenseCategory? category = null)
    {
        var db = await _db.GetConnectionAsync();

        // ── Build date range from the supplied filter params ──
        DateTime? filterStart = null;
        DateTime? filterEnd = null;

        if (rangeStart.HasValue && rangeEnd.HasValue)
        {
            filterStart = rangeStart.Value;
            filterEnd = rangeEnd.Value;
        }
        else if (year.HasValue && month.HasValue && day.HasValue)
        {
            filterStart = new DateTime(year.Value, month.Value, day.Value);
            filterEnd = filterStart.Value.AddDays(1);
        }
        else if (year.HasValue && month.HasValue)
        {
            filterStart = new DateTime(year.Value, month.Value, 1);
            filterEnd = filterStart.Value.AddMonths(1);
        }
        else if (year.HasValue)
        {
            filterStart = new DateTime(year.Value, 1, 1);
            filterEnd = filterStart.Value.AddYears(1);
        }

        // ── Standalone expenses (GroceryGroupId IS NULL) ──
        List<ExpenseEntity> expenses;
        if (filterStart.HasValue)
        {
            expenses = await db.QueryAsync<ExpenseEntity>(
                "SELECT * FROM expenses WHERE GroceryGroupId IS NULL AND Date >= ? AND Date < ?",
                filterStart.Value, filterEnd!.Value);
        }
        else
        {
            expenses = await db.Table<ExpenseEntity>()
                .Where(e => e.GroceryGroupId == null)
                .ToListAsync();
        }

        // ── Grocery groups ──
        List<GroceryGroupEntity> groups;
        if (filterStart.HasValue)
        {
            groups = await db.QueryAsync<GroceryGroupEntity>(
                "SELECT * FROM grocery_groups WHERE Date >= ? AND Date < ?",
                filterStart.Value, filterEnd!.Value);
        }
        else
        {
            groups = await db.Table<GroceryGroupEntity>().ToListAsync();
        }

        // ── Line items — only fetch those belonging to matched groups ──
        List<ExpenseEntity> lineItems;
        if (groups.Count > 0)
        {
            var groupIds = groups.Select(g => g.Id).ToHashSet();
            // sqlite-net doesn't support IN clause via LINQ, fetch and filter
            var allGrouped = await db.Table<ExpenseEntity>()
                .Where(e => e.GroceryGroupId != null)
                .ToListAsync();
            lineItems = allGrouped.Where(e => groupIds.Contains(e.GroceryGroupId!.Value)).ToList();
        }
        else
        {
            lineItems = [];
        }

        var cards = new List<FeedCardItem>();
        var currencySymbol = _settings.CurrencySymbol;

        foreach (var e in expenses)
        {
            var expCat = ParseCategory(e.Category);
            if (category.HasValue && expCat != category.Value) continue;
            cards.Add(new FeedCardItem
            {
                Id = e.Id,
                CurrencySymbol = currencySymbol,
                Amount = e.Amount,
                Category = expCat,
                Title = e.Title,
                Note = e.Note,
                Date = e.Date,
                PhotoPath = e.PhotoPath,
                RegretRating = e.RegretRating
            });
        }

        var byGroup = lineItems
            .GroupBy(x => x.GroceryGroupId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var group in groups)
        {
            if (category.HasValue && category.Value != ExpenseCategory.Grocery) continue;

            var lines = byGroup.TryGetValue(group.Id, out var l) ? l : [];
            var items = lines.Select(li => new GroceryLineItem
            {
                Id = li.Id,
                GroceryGroupId = group.Id,
                Name = li.Title ?? li.Note ?? "Item",
                CurrencySymbol = currencySymbol,
                Price = li.Amount
            }).ToList();

            var card = new FeedCardItem
            {
                Id = group.Id,
                IsGroceryGroup = true,
                Category = ExpenseCategory.Grocery,
                Note = group.Note,
                Date = group.Date,
                CurrencySymbol = currencySymbol,
                GroceryItems = new System.Collections.ObjectModel.ObservableCollection<GroceryLineItem>(items)
            };
            card.Amount = items.Sum(i => i.Price);
            cards.Add(card);
        }

        return cards.OrderByDescending(c => c.Date);
    }

    public async Task DeleteAllAsync()
    {
        var db = await _db.GetConnectionAsync();

        // Collect photo paths before wiping — we clean up files after the DB commit
        // so a failed delete doesn't leave the DB in a partially-deleted state.
        var photoPaths = (await db.Table<ExpenseEntity>()
            .Where(e => e.PhotoPath != null)
            .ToListAsync())
            .Select(e => e.PhotoPath!)
            .ToList();

        await db.DeleteAllAsync<ExpenseEntity>();
        await db.DeleteAllAsync<GroceryGroupEntity>();

        DeletePhotoFiles(photoPaths);
    }

    // Fetches the raw DB row by Id — used by edit mode to pre-fill the form
    public async Task<ExpenseEntity?> GetExpenseByIdAsync(int id)
    {
        var db = await _db.GetConnectionAsync();
        return await db.Table<ExpenseEntity>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task SaveExpenseAsync(ExpenseEntity expense)
    {
        var db = await _db.GetConnectionAsync();
        await db.InsertAsync(expense);
    }

    public async Task UpdateExpenseAsync(ExpenseEntity expense)
    {
        expense.UpdatedAt = DateTime.Now;
        var db = await _db.GetConnectionAsync();
        await db.UpdateAsync(expense);
    }

    public async Task DeleteExpenseAsync(int id)
    {
        var db = await _db.GetConnectionAsync();

        // Read the photo path before deleting the row so we can clean up the file.
        var entity = await db.Table<ExpenseEntity>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();

        await db.DeleteAsync<ExpenseEntity>(id);

        // Delete the copied photo file if one exists. Failures are swallowed so a
        // locked / already-missing file never blocks the logical delete.
        if (entity?.PhotoPath is { } path)
            DeletePhotoFiles([path]);
    }

    public async Task DeleteGroceryGroupAsync(int groupId)
    {
        var db = await _db.GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM expenses WHERE GroceryGroupId = ?", groupId);
        await db.DeleteAsync<GroceryGroupEntity>(groupId);
        // Grocery groups don't carry photos — no file cleanup needed here.
    }

    public async Task DeleteGroceryLineItemAsync(int lineItemId)
    {
        var db = await _db.GetConnectionAsync();
        await db.DeleteAsync<ExpenseEntity>(lineItemId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DeletePhotoFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                // Log but never surface — a stuck/missing file must not break the delete flow.
                System.Diagnostics.Debug.WriteLine($"Photo cleanup failed for {path}: {ex.Message}");
            }
        }
    }

    private static ExpenseCategory ParseCategory(string raw) =>
        Enum.TryParse<ExpenseCategory>(raw, true, out var cat) ? cat : ExpenseCategory.Other;

    public async Task<List<int>> GetDistinctExpenseYearsAsync()
    {
        var db = await _db.GetConnectionAsync();
        var expenseYears = await db.QueryAsync<YearRow>(
            "SELECT DISTINCT CAST(strftime('%Y', Date) AS INTEGER) AS Y FROM expenses WHERE Date IS NOT NULL");
        var groupYears = await db.QueryAsync<YearRow>(
            "SELECT DISTINCT CAST(strftime('%Y', Date) AS INTEGER) AS Y FROM grocery_groups WHERE Date IS NOT NULL");

        // Always show at least the last 6 years; if there is older data, include those too.
        var currentYear = DateTime.Now.Year;
        var defaultYears = Enumerable.Range(currentYear - 5, 6);

        return expenseYears.Select(r => r.Y)
            .Concat(groupYears.Select(r => r.Y))
            .Concat(defaultYears)
            .Where(y => y > 0)           // guard against null dates stored as 0
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();
    }

    private class YearRow
    {
        public int Y { get; set; }
    }
}