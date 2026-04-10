using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IWishService
{
    Task<IEnumerable<WishCardItem>> GetWishItemsAsync();
    Task<int> SaveWishItemAsync(WishItemEntity entity);
    Task UpdateStatusAsync(int id, WishStatus status);
    Task SetRegretAsync(int id, string rating);
    Task PinItemAsync(int id);
    Task ConvertToExpenseAsync(int id);
    Task DeleteWishItemAsync(int id);
    Task<bool> IsDuplicateAsync(string name);
}

// ── Implementation ────────────────────────────────────────────────────────────

public class WishService : IWishService
{
    private readonly DatabaseService _db;

    public WishService(DatabaseService db) => _db = db;

    public async Task<IEnumerable<WishCardItem>> GetWishItemsAsync()
    {
        var db = await _db.GetConnectionAsync();
        var entities = await db.Table<WishItemEntity>()
            .OrderByDescending(w => w.IsPinned)
            .ToListAsync();

        // Sort: pinned first, then by creation date descending
        return entities
            .OrderByDescending(w => w.IsPinned)
            .ThenByDescending(w => w.CreatedAt)
            .Select(w => new WishCardItem
            {
                Id = w.Id,
                Name = w.Name,
                Price = w.Price,
                Priority = ParsePriority(w.Priority),
                Category = ParseCategory(w.Category),
                Caption = w.Caption,
                PhotoPath = w.PhotoPath,
                TargetMonth = w.TargetMonth,
                Status = ParseStatus(w.Status),
                RegretRating = w.RegretRating,
                IsPinned = w.IsPinned,
                CreatedAt = w.CreatedAt
            });
    }

    public async Task<int> SaveWishItemAsync(WishItemEntity entity)
    {
        var db = await _db.GetConnectionAsync();
        await db.InsertAsync(entity);
        return entity.Id;
    }

    public async Task UpdateStatusAsync(int id, WishStatus status)
    {
        var db = await _db.GetConnectionAsync();
        var item = await db.Table<WishItemEntity>().Where(w => w.Id == id).FirstOrDefaultAsync();
        if (item is null) return;

        item.Status = status.ToString();
        item.UpdatedAt = DateTime.Now;
        await db.UpdateAsync(item);
    }

    public async Task SetRegretAsync(int id, string rating)
    {
        var db = await _db.GetConnectionAsync();
        var item = await db.Table<WishItemEntity>().Where(w => w.Id == id).FirstOrDefaultAsync();
        if (item is null) return;

        item.RegretRating = rating;
        item.UpdatedAt = DateTime.Now;
        await db.UpdateAsync(item);
    }

    public async Task PinItemAsync(int id)
    {
        var db = await _db.GetConnectionAsync();
        var items = await db.Table<WishItemEntity>().ToListAsync();

        // Unpin all others first — only one item can be pinned at a time
        foreach (var w in items)
        {
            var wasPinned = w.IsPinned;
            w.IsPinned = w.Id == id ? !w.IsPinned : false;
            if (wasPinned != w.IsPinned)
                await db.UpdateAsync(w);
        }
    }

    public async Task ConvertToExpenseAsync(int id)
    {
        var db = await _db.GetConnectionAsync();
        var item = await db.Table<WishItemEntity>().Where(w => w.Id == id).FirstOrDefaultAsync();
        if (item is null) return;

        // Insert as a standalone expense
        await db.InsertAsync(new ExpenseEntity
        {
            Title = item.Name,
            Amount = item.Price,
            Category = item.Category,
            Note = item.Caption,
            PhotoPath = item.PhotoPath,
            Date = DateTime.Now
        });

        // Remove from wishlist after converting
        await db.DeleteAsync<WishItemEntity>(id);
    }

    public async Task DeleteWishItemAsync(int id)
    {
        var db = await _db.GetConnectionAsync();
        await db.DeleteAsync<WishItemEntity>(id);
    }

    public async Task<bool> IsDuplicateAsync(string name)
    {
        var db = await _db.GetConnectionAsync();
        var items = await db.Table<WishItemEntity>().ToListAsync();
        return items.Any(w => string.Equals(w.Name.Trim(), name.Trim(),
            StringComparison.OrdinalIgnoreCase));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WishPriority ParsePriority(string raw) =>
        Enum.TryParse<WishPriority>(raw, true, out var p) ? p : WishPriority.Want;

    private static WishStatus ParseStatus(string raw) =>
        Enum.TryParse<WishStatus>(raw, true, out var s) ? s : WishStatus.Planned;

    private static ExpenseCategory ParseCategory(string raw) =>
        Enum.TryParse<ExpenseCategory>(raw, true, out var c) ? c : ExpenseCategory.Other;
}