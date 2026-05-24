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

        var expenses = await db.Table<ExpenseEntity>()
            .Where(e => e.GroceryGroupId == null)
            .ToListAsync();

        var groups = await db.Table<GroceryGroupEntity>().ToListAsync();

        var lineItems = await db.Table<ExpenseEntity>()
            .Where(e => e.GroceryGroupId != null)
            .ToListAsync();

        var cards = new List<FeedCardItem>();
        var currencySymbol = _settings.CurrencySymbol;

        foreach (var e in expenses)
        {
            if (!Matches(e.Date, year, month, day, rangeStart, rangeEnd)) continue;
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
                PhotoPath = e.PhotoPath
            });
        }

        var byGroup = lineItems
            .GroupBy(x => x.GroceryGroupId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var group in groups)
        {
            if (!Matches(group.Date, year, month, day, rangeStart, rangeEnd)) continue;
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

        // Delete all expense rows (including grocery line items)
        await db.DeleteAllAsync<ExpenseEntity>();

        // Delete all grocery groups
        await db.DeleteAllAsync<GroceryGroupEntity>();
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
        await db.DeleteAsync<ExpenseEntity>(id);
    }

    public async Task DeleteGroceryGroupAsync(int groupId)
    {
        var db = await _db.GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM expenses WHERE GroceryGroupId = ?", groupId);
        await db.DeleteAsync<GroceryGroupEntity>(groupId);
    }

    public async Task DeleteGroceryLineItemAsync(int lineItemId)
    {
        var db = await _db.GetConnectionAsync();
        await db.DeleteAsync<ExpenseEntity>(lineItemId);
    }

    private static bool Matches(DateTime date, int? year, int? month, int? day = null,
        DateTime? rangeStart = null, DateTime? rangeEnd = null)
    {
        if (year.HasValue && date.Year != year.Value) return false;
        if (month.HasValue && date.Month != month.Value) return false;
        if (day.HasValue && date.Day != day.Value) return false;
        if (rangeStart.HasValue && date < rangeStart.Value) return false;
        if (rangeEnd.HasValue && date >= rangeEnd.Value) return false;
        return true;
    }

    private static ExpenseCategory ParseCategory(string raw) =>
        Enum.TryParse<ExpenseCategory>(raw, true, out var cat) ? cat : ExpenseCategory.Other;
}