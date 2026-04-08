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