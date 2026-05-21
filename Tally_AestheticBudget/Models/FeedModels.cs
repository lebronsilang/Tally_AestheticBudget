using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tally_AestheticBudget.Models;

public enum ExpenseCategory
{
    Food, Transport, Shopping, Health, Fun, Other,
    Grocery
}

public partial class FeedCardItem : ObservableObject
{
    public int Id { get; set; }
    public bool IsGroceryGroup { get; set; }
    public ObservableCollection<GroceryLineItem> GroceryItems { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AmountFormatted))]
    [NotifyPropertyChangedFor(nameof(GroceryItemCountLabel))]
    private decimal _amount;

    public ExpenseCategory Category { get; set; }

    // Short label — shown prominently on the card
    public string? Title { get; set; }

    // Longer detail — shown in the bottom sheet only
    public string? Note { get; set; }

    public DateTime Date { get; set; }
    public string? PhotoPath { get; set; }

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    public bool HasNote => !string.IsNullOrEmpty(Note);

    public string CurrencySymbol { get; set; } = "₱";
    public string AmountFormatted => $"{CurrencySymbol}{Amount:N2}";
    public string DateFormatted => Date.ToString("dd MMM yyyy");

    public string CategoryLabel => IsGroceryGroup ? "Grocery" : Category switch
    {
        ExpenseCategory.Food => "Food",
        ExpenseCategory.Transport => "Transport",
        ExpenseCategory.Shopping => "Shopping",
        ExpenseCategory.Health => "Health",
        ExpenseCategory.Fun => "Fun",
        _ => "Other"
    };

    public string GroceryItemCountLabel =>
        GroceryItems.Count == 1 ? "1 item" : $"{GroceryItems.Count} items";

    public void RecalculateFromGroceryItems()
    {
        Amount = GroceryItems.Sum(i => i.Price * i.Quantity);
        OnPropertyChanged(nameof(GroceryItemCountLabel));
    }
}

public class GroceryLineItem
{
    public int Id { get; set; }
    public int GroceryGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public string CurrencySymbol { get; set; } = "₱";
    public string PriceFormatted => $"{CurrencySymbol}{Price * Quantity:N2}";
}

public class MonthOption
{
    public int Month { get; set; }
    public string ShortName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}