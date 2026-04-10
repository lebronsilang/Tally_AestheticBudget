using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IGroceryService
{
    Task<IEnumerable<GroceryItem>> GetItemsAsync();
    Task AddItemAsync(string name, decimal price, int quantity);
    Task ToggleCheckedAsync(int id);
    Task DeleteItemAsync(int id);

    // Converts all checked items into a grocery group expense entry.
    // Returns the new group's Id so the Feed can highlight it.
    Task<int> BuyCheckedAsync();

    // Returns how much has been spent on Grocery this month
    Task<decimal> GetGrocerySpentThisMonthAsync();
}

// ── Implementation ────────────────────────────────────────────────────────────

public class GroceryService : IGroceryService
{
    private readonly DatabaseService _db;

    public GroceryService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<IEnumerable<GroceryItem>> GetItemsAsync()
    {
        var db = await _db.GetConnectionAsync();
        var entities = await db.Table<GroceryItemEntity>()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return entities.Select(e => new GroceryItem
        {
            Id = e.Id,
            Name = e.Name,
            Price = e.Price,
            Quantity = e.Quantity,
            IsChecked = e.IsChecked,
            CreatedAt = e.CreatedAt
        });
    }

    public async Task AddItemAsync(string name, decimal price, int quantity)
    {
        var db = await _db.GetConnectionAsync();
        await db.InsertAsync(new GroceryItemEntity
        {
            Name = name.Trim(),
            Price = price,
            Quantity = quantity,
            IsChecked = false,
            CreatedAt = DateTime.Now
        });
    }

    public async Task ToggleCheckedAsync(int id)
    {
        var db = await _db.GetConnectionAsync();
        var item = await db.Table<GroceryItemEntity>()
            .Where(g => g.Id == id)
            .FirstOrDefaultAsync();

        if (item is null) return;
        item.IsChecked = !item.IsChecked;
        await db.UpdateAsync(item);
    }

    public async Task DeleteItemAsync(int id)
    {
        var db = await _db.GetConnectionAsync();
        await db.DeleteAsync<GroceryItemEntity>(id);
    }

    public async Task<int> BuyCheckedAsync()
    {
        var db = await _db.GetConnectionAsync();

        // Get all checked items — including those with ₱0
        var checkedEntities = await db.Table<GroceryItemEntity>()
            .Where(g => g.IsChecked)
            .ToListAsync();

        if (checkedEntities.Count == 0) return 0;

        // Create a grocery group header
        var group = new GroceryGroupEntity
        {
            Date = DateTime.Now,
            Note = $"Grocery run · {checkedEntities.Count} item{(checkedEntities.Count == 1 ? "" : "s")}"
        };
        await db.InsertAsync(group);

        // Insert each checked item as an expense line under this group
        foreach (var item in checkedEntities)
        {
            await db.InsertAsync(new ExpenseEntity
            {
                Amount = item.Price * item.Quantity,
                Title = item.Name,
                Note = item.Quantity > 1 ? $"qty: {item.Quantity}" : null,
                Category = ExpenseCategory.Grocery.ToString(),
                Date = DateTime.Now,
                GroceryGroupId = group.Id
            });
        }

        // Remove only the checked items from the grocery list
        foreach (var item in checkedEntities)
            await db.DeleteAsync<GroceryItemEntity>(item.Id);

        return group.Id;
    }

    public async Task<decimal> GetGrocerySpentThisMonthAsync()
    {
        var db = await _db.GetConnectionAsync();
        var now = DateTime.Now;
        var month = now.Month;
        var year = now.Year;

        // Get all grocery group headers this month
        var groups = await db.Table<GroceryGroupEntity>()
            .ToListAsync();

        var thisMonthGroupIds = groups
            .Where(g => g.Date.Month == month && g.Date.Year == year)
            .Select(g => g.Id)
            .ToHashSet();

        if (thisMonthGroupIds.Count == 0) return 0;

        // Sum all line items under those groups
        var lineItems = await db.Table<ExpenseEntity>()
            .Where(e => e.GroceryGroupId != null)
            .ToListAsync();

        return lineItems
            .Where(e => thisMonthGroupIds.Contains(e.GroceryGroupId!.Value))
            .Sum(e => e.Amount);
    }
}