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
    private readonly ISettingsService _settings;
    private readonly DataChangedService _dataChanged;
    private readonly IThemeService _themeService;
    private readonly HeaderState _header;
    private bool _themeSubscribed;

    private List<GroceryItem> _allItems = [];

    public GroceryViewModel(
        IGroceryService groceryService,
        IBudgetService budgetService,
        ISettingsService settings,
        DataChangedService dataChanged,
        IThemeService themeService,
        HeaderState header)
    {
        _groceryService = groceryService;
        _budgetService = budgetService;
        _settings = settings;
        _dataChanged = dataChanged;
        _themeService = themeService;

        _dataChanged.GroceryChanged += () => IsDirty = true;
        _dataChanged.SettingsChanged += () =>
        {
            IsDirty = true;
            OnPropertyChanged(nameof(ExpensePanelOnLeft));
        };
        _header = header;
    }

    public bool ExpensePanelOnLeft => _settings.ExpensePanelOnLeft;
    public string PriceLabelText => $"Price ({_settings.CurrencySymbol}) optional";

    // ── Items ─────────────────────────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<GroceryItem> _displayedItems = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statsLabel = "0 items - 0.00 total";
    [ObservableProperty] private GroceryBudgetStatus _budgetStatus = new();

    // ── Filter ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterPending))]
    [NotifyPropertyChangedFor(nameof(IsFilterChecked))]
    private GroceryFilter _activeFilter = GroceryFilter.All;

    public bool IsFilterAll => ActiveFilter == GroceryFilter.All;
    public bool IsFilterPending => ActiveFilter == GroceryFilter.Pending;
    public bool IsFilterChecked => ActiveFilter == GroceryFilter.Checked;

    private string FilterHeaderLabel => ActiveFilter switch
    {
        GroceryFilter.Pending => "Pending",
        GroceryFilter.Checked => "Checked",
        _ => string.Empty
    };
    // ── Add modal ─────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isAddModalVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveItemCommand))]
    private string _newItemName = string.Empty;

    [ObservableProperty] private string _newItemPrice = string.Empty;
    [ObservableProperty] private string _newItemQuantity = "1";

    private bool CanSaveItem() => !string.IsNullOrWhiteSpace(NewItemName);

    // ── Filter commands ───────────────────────────────────────────────────────

    [RelayCommand] private void FilterAll() { ActiveFilter = GroceryFilter.All; ApplyFilter(); }
    [RelayCommand] private void FilterPending() { ActiveFilter = GroceryFilter.Pending; ApplyFilter(); }
    [RelayCommand] private void FilterChecked() { ActiveFilter = GroceryFilter.Checked; ApplyFilter(); }

    // ── Add modal commands ────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddModal()
    {
        NewItemName = string.Empty;
        NewItemPrice = string.Empty;
        NewItemQuantity = "1";
        OnPropertyChanged(nameof(PriceLabelText));
        IsAddModalVisible = true;
    }

    [RelayCommand] private void DismissAddModal() => IsAddModalVisible = false;

    [RelayCommand(CanExecute = nameof(CanSaveItem))]
    private async Task SaveItemAsync()
    {
        decimal.TryParse(NewItemPrice, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var price);
        int.TryParse(NewItemQuantity, out var qty);
        if (qty < 1) qty = 1;

        await _groceryService.AddItemAsync(NewItemName, price, qty);
        IsAddModalVisible = false;
        IsDirty = true;
        await LoadAsync();
    }


    // ── List commands ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleCheckedAsync(GroceryItem item)
    {
        await _groceryService.ToggleCheckedAsync(item.Id);
        item.IsChecked = !item.IsChecked;
        UpdateBudgetPending();
        UpdateStats();
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
                "Nothing checked", "Check off items you've bought first.", "OK");
            return;
        }

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Buy Checked",
            $"Convert {checkedCount} item{(checkedCount == 1 ? "" : "s")} to expenses?",
            "Buy", "Cancel");
        if (!confirmed) return;

        await _groceryService.BuyCheckedAsync();
        _dataChanged.NotifyGroceryChanged();
        IsDirty = true;
        await LoadAsync();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public bool IsDirty { get; set; } = true;

    public async Task OnPageAppearingAsync()
    {
        if (!_themeSubscribed)
        {
            _themeSubscribed = true;
            _themeService.ThemeChanged += OnThemeChanged;
        }
        _header.ShowFilter(FilterHeaderLabel);
        if (!IsDirty) return;
        IsDirty = false;
        await LoadAsync();
    }

    public void OnPageDisappearing()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _themeSubscribed = false;
    }

    private void OnThemeChanged()
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterPending));
        OnPropertyChanged(nameof(IsFilterChecked));
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _groceryService.GetItemsAsync();
            _allItems = items.ToList();
            var sym = _settings.CurrencySymbol;

            var now = DateTime.Now;
            var budgets = await _budgetService.GetBudgetItemsAsync(now.Year, now.Month);
            var groceryBudget = budgets.FirstOrDefault(b => b.Category == ExpenseCategory.Grocery);
            var spent = await _groceryService.GetGrocerySpentThisMonthAsync();

            BudgetStatus.CurrencySymbol = sym;
            BudgetStatus.BudgetLimit = groceryBudget?.Limit ?? 0;
            BudgetStatus.AlreadySpent = spent;
            foreach (var item in _allItems)
                item.CurrencySymbol = sym;

            UpdateBudgetPending();
            UpdateStats();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadGrocery failed: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Error",
                "Could not load grocery data. Please try again.", "OK");
        }
        finally { IsLoading = false; }
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
        _header.ShowFilter(FilterHeaderLabel);
    }

    private void UpdateStats()
    {
        var sym = _settings.CurrencySymbol;
        var count = _allItems.Count;
        var total = _allItems.Sum(i => i.Price * i.Quantity);
        StatsLabel = $"{count} item{(count == 1 ? "" : "s")} · {sym}{total:N2} total";
    }

    private void UpdateBudgetPending()
    {
        BudgetStatus.PendingTotal = _allItems
            .Where(i => i.IsChecked)
            .Sum(i => i.Price * i.Quantity);
    }
}