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

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock — another caller may have initialized it
            if (_connection is not null)
                return _connection;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "budget.db");

            var conn = new SQLiteAsyncConnection(dbPath);

            await conn.CreateTableAsync<ExpenseEntity>();
            await conn.CreateTableAsync<GroceryGroupEntity>();
            await conn.CreateTableAsync<BudgetEntity>();
            await conn.CreateTableAsync<GroceryItemEntity>();
            await conn.CreateTableAsync<WishItemEntity>();

            _connection = conn; // publish only after tables are created
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }
}