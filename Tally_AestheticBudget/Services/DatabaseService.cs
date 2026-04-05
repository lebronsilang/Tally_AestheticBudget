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

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        // AppDataDirectory is the private folder MAUI gives each app.
        // On Windows: C:\Users\You\AppData\Local\Packages\...\budget.db
        // On Android: /data/data/com.yourapp/files/budget.db
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "budget.db");

        _connection = new SQLiteAsyncConnection(dbPath);

        // CreateTableAsync is safe to call every launch —
        // it only creates the table if it doesn't already exist.
        await _connection.CreateTableAsync<ExpenseEntity>();
        await _connection.CreateTableAsync<GroceryGroupEntity>();

        return _connection;
    }
}