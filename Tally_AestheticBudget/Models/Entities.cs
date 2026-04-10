using SQLite;

namespace Tally_AestheticBudget.Models;

/// <summary>
/// Maps to the "expenses" table in SQLite.
/// Each row is one expense — or one grocery line item if GroceryGroupId is set.
/// </summary>
[Table("expenses")]
public class ExpenseEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    // Short label shown on the feed card e.g. "Lunch with friends"
    public string? Title { get; set; }

    // Longer optional detail shown in the bottom sheet e.g. "Had ramen near school"
    public string? Note { get; set; }

    // Stored as a string e.g. "Food", "Transport" — parsed back to enum on read
    public string Category { get; set; } = string.Empty;

    public string? PhotoPath { get; set; }

    public DateTime Date { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Null = standalone expense.
    // Non-null = this row is a line item inside a grocery group with this Id.
    public int? GroceryGroupId { get; set; }
}

/// <summary>
/// Maps to the "grocery_groups" table.
/// One row per "Buy Checked" run from the Grocery screen.
/// </summary>
[Table("grocery_groups")]
public class GroceryGroupEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// One row per category per month.
/// e.g. Food · April 2026 · limit ₱3,000
/// </summary>
[Table("budgets")]
public class BudgetEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

/// <summary>
/// Maps to the "grocery_items" table.
/// These are the PENDING items in the grocery list —
/// separate from expense entries which live in the expenses table.
/// </summary>
[Table("grocery_items")]
public class GroceryItemEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsChecked { get; set; }
    public DateTime CreatedAt { get; set; }
}

[Table("wish_items")]
public class WishItemEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Priority { get; set; } = "Want";  // Want / Need / Someday
    public string Category { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? PhotoPath { get; set; }
    public string? TargetMonth { get; set; }         // "2026-06" format
    public string Status { get; set; } = "Planned";  // Planned / Bought
    public string? RegretRating { get; set; }         // Worth / Regret
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}