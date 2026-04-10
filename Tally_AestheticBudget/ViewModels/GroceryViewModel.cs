using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class GroceryViewModel : ObservableObject
{
    private readonly IGroceryService _groceryService;
    private readonly IBudgetService _budgetService;

    // Full unfiltered list — filtering happens in ApplyFilter()
    private List<GroceryItem> _allItems = [];

    public GroceryViewModel(IGroceryService groceryService, IBudgetService budgetService)
    {
        _groceryService = groceryService;
        _budgetService = budgetService;
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<GroceryItem> _displayedItems = [];

    [ObservableProperty]
    private bool _isLoading;

    // ── Stats ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _statsLabel = "0 items · ₱0.00 total";

    [ObservableProperty]
    private GroceryBudgetStatus _budgetStatus = new();

    // ── Filter ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterPending))]
    [NotifyPropertyChangedFor(nameof(IsFilterChecked))]
    private GroceryFilter _activeFilter = GroceryFilter.All;

    public bool IsFilterAll => ActiveFilter == GroceryFilter.All;
    public bool IsFilterPending => ActiveFilter == GroceryFilter.Pending;
    public bool IsFilterChecked => ActiveFilter == GroceryFilter.Checked;

    // ── Add item modal ────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isAddModalVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveItemCommand))]
    private string _newItemName = string.Empty;

    [ObservableProperty]
    private string _newItemPrice = string.Empty;

    [ObservableProperty]
    private string _newItemQuantity = "1";

    private bool CanSaveItem() => !string.IsNullOrWhiteSpace(NewItemName);

    // ── Commands — filter ─────────────────────────────────────────────────────

    [RelayCommand]
    private void FilterAll()
    {
        ActiveFilter = GroceryFilter.All;
        ApplyFilter();
    }

    [RelayCommand]
    private void FilterPending()
    {
        ActiveFilter = GroceryFilter.Pending;
        ApplyFilter();
    }

    [RelayCommand]
    private void FilterChecked()
    {
        ActiveFilter = GroceryFilter.Checked;
        ApplyFilter();
    }

    // ── Commands — add modal ──────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddModal()
    {
        NewItemName = string.Empty;
        NewItemPrice = string.Empty;
        NewItemQuantity = "1";
        IsAddModalVisible = true;
    }

    [RelayCommand]
    private void DismissAddModal() => IsAddModalVisible = false;

    [RelayCommand(CanExecute = nameof(CanSaveItem))]
    private async Task SaveItemAsync()
    {
        decimal.TryParse(NewItemPrice, out var price);
        int.TryParse(NewItemQuantity, out var qty);
        if (qty < 1) qty = 1;

        await _groceryService.AddItemAsync(NewItemName, price, qty);
        IsAddModalVisible = false;
        await LoadAsync();
    }

    // ── Commands — list actions ───────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleCheckedAsync(GroceryItem item)
    {
        await _groceryService.ToggleCheckedAsync(item.Id);
        item.IsChecked = !item.IsChecked;

        // Recalculate pending total for budget bar live update
        UpdateBudgetPending();
        UpdateStats();

        // Re-apply filter so checked items move to correct section
        ApplyFilter();
    }

    [RelayCommand]
    private async Task DeleteItemAsync(GroceryItem item)
    {
        await _groceryService.DeleteItemAsync(item.Id);
        _allItems.Remove(item);
        UpdateStats();
        UpdateBudgetPending();
        ApplyFilter();
    }

    [RelayCommand]
    private async Task BuyCheckedAsync()
    {
        var checkedCount = _allItems.Count(i => i.IsChecked);
        if (checkedCount == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Nothing checked",
                "Check off items you've bought first.",
                "OK");
            return;
        }

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Buy Checked",
            $"Convert {checkedCount} item{(checkedCount == 1 ? "" : "s")} to expenses?",
            "Buy", "Cancel");

        if (!confirmed) return;

        await _groceryService.BuyCheckedAsync();
        await LoadAsync();
    }

    // ── Page lifecycle ────────────────────────────────────────────────────────

    public async Task OnPageAppearingAsync()
    {
        await LoadAsync();
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _groceryService.GetItemsAsync();
            _allItems = items.ToList();

            // Load grocery budget status
            var now = DateTime.Now;
            var budget = await _budgetService.GetBudgetItemsAsync(now.Year, now.Month);
            var groceryBudget = budget.FirstOrDefault(b => b.Category == ExpenseCategory.Grocery);

            var spent = await _groceryService.GetGrocerySpentThisMonthAsync();

            BudgetStatus.BudgetLimit = groceryBudget?.Limit ?? 0;
            BudgetStatus.AlreadySpent = spent;
            UpdateBudgetPending();

            UpdateStats();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = ActiveFilter switch
        {
            GroceryFilter.Pending => _allItems.Where(i => !i.IsChecked),
            GroceryFilter.Checked => _allItems.Where(i => i.IsChecked),
            _ => _allItems.AsEnumerable()
        };

        DisplayedItems = new ObservableCollection<GroceryItem>(filtered);
    }

    private void UpdateStats()
    {
        var count = _allItems.Count;
        var total = _allItems.Sum(i => i.Price * i.Quantity);
        StatsLabel = $"{count} item{(count == 1 ? "" : "s")} · ₱{total:N2} total";
    }

    private void UpdateBudgetPending()
    {
        var pending = _allItems
            .Where(i => i.IsChecked)
            .Sum(i => i.Price * i.Quantity);
        BudgetStatus.PendingTotal = pending;
    }
}