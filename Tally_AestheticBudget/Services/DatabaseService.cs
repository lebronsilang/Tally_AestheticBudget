using SQLite;
using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

/// <summary>
/// Opens and holds the single SQLite connection for the app.
/// Call GetConnectionAsync() from any service that needs the DB.
/// The connection is created once and reused (singleton pattern).
/// </summary>
public class DatabaseService
{
    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Bump the version suffix if a future migration needs to run again.
    private const string BudgetNullableMigrationKey = "mig_budget_nullable_v1";

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync();
        try
        {
            if (_connection is not null)
                return _connection;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "budget.db");

            var conn = new SQLiteAsyncConnection(dbPath);

            await conn.CreateTableAsync<ExpenseEntity>();
            await conn.CreateTableAsync<GroceryGroupEntity>();
            await conn.CreateTableAsync<BudgetEntity>();
            await conn.CreateTableAsync<GroceryItemEntity>();
            await conn.CreateTableAsync<WishItemEntity>();
            await conn.CreateTableAsync<MonthlyThemeEntity>();

            await MigrateBudgetsToNullableAsync(conn);

            _connection = conn; // publish only after tables + migrations are done
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// One-time migration for item 5. Before this change, a stored limit of 0
    /// meant "no limit set". The domain now treats 0 as an intentional ₱0 cap and
    /// encodes "Unlimited" as the sentinel -1. To preserve the original intent of
    /// existing data, rewrite every legacy 0 row to -1 exactly once.
    ///
    /// Gated by a Preferences flag so a user's *future* intentional ₱0 caps are
    /// never clobbered. Done through the ORM (not raw SQL) so it is agnostic to
    /// how sqlite-net serializes decimals.
    /// </summary>
    private static async Task MigrateBudgetsToNullableAsync(SQLiteAsyncConnection conn)
    {
        if (Preferences.Get(BudgetNullableMigrationKey, false))
            return;

        try
        {
            var rows = await conn.Table<BudgetEntity>().ToListAsync();
            var legacyZeros = rows.Where(r => r.Limit == 0m).ToList();

            foreach (var r in legacyZeros)
                r.Limit = -1m; // Unlimited sentinel

            if (legacyZeros.Count > 0)
                await conn.UpdateAllAsync(legacyZeros);

            Preferences.Set(BudgetNullableMigrationKey, true);
        }
        catch (Exception ex)
        {
            // Don't block startup on a migration hiccup; it will retry next launch
            // because the flag is only set on success.
            System.Diagnostics.Debug.WriteLine($"Budget nullable migration failed: {ex.Message}");
        }
    }
}