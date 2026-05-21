using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class WishlistViewModel : ObservableObject
{
    private readonly IWishService _wishService;
    private readonly IBudgetService _budgetService;
    private readonly IExpenseService _expenseService;
    private readonly ISettingsService _settings;

    private List<WishCardItem> _allItems = [];

    public WishlistViewModel(
        IWishService wishService,
        IBudgetService budgetService,
        IExpenseService expenseService,
        ISettingsService settings)
    {
        _wishService = wishService;
        _budgetService = budgetService;
        _expenseService = expenseService;
        _settings = settings;
    }

    // Exposed so XAML can bind the currency label dynamically
    public string CurrencySymbol => _settings.CurrencySymbol;
    public string PriceLabelText => $"Price ({_settings.CurrencySymbol})";

    // ── Items ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<WishCardItem> _displayedItems = [];

    public ObservableCollection<ObservableCollection<WishCardItem>> Columns { get; } = [];

    public event Action? ColumnsRebuilt;
    public event Action? DataLoaded;

    public void DistributeIntoColumns(int columnCount)
    {
        Columns.Clear();
        for (int i = 0; i < columnCount; i++)
            Columns.Add([]);

        int col = 0;
        foreach (var item in DisplayedItems)
        {
            Columns[col].Add(item);
            col = (col + 1) % columnCount;
        }

        ColumnsRebuilt?.Invoke();
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statsLabel = "0 planned · 0 bought";

    // ── Filter ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterPlanned))]
    [NotifyPropertyChangedFor(nameof(IsFilterBought))]
    [NotifyPropertyChangedFor(nameof(IsFilterWant))]
    [NotifyPropertyChangedFor(nameof(IsFilterNeed))]
    [NotifyPropertyChangedFor(nameof(IsFilterSomeday))]
    private WishFilter _activeFilter = WishFilter.All;

    public bool IsFilterAll => ActiveFilter == WishFilter.All;
    public bool IsFilterPlanned => ActiveFilter == WishFilter.Planned;
    public bool IsFilterBought => ActiveFilter == WishFilter.Bought;
    public bool IsFilterWant => ActiveFilter == WishFilter.Want;
    public bool IsFilterNeed => ActiveFilter == WishFilter.Need;
    public bool IsFilterSomeday => ActiveFilter == WishFilter.Someday;

    // ── Add modal ─────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isAddModalVisible;
    [ObservableProperty] private string _newName = string.Empty;
    [ObservableProperty] private string _newPrice = string.Empty;
    [ObservableProperty] private string _newCaption = string.Empty;
    [ObservableProperty] private string _newTargetMonth = string.Empty;
    [ObservableProperty] private string? _newPhotoPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWantSelected))]
    [NotifyPropertyChangedFor(nameof(IsNeedSelected))]
    [NotifyPropertyChangedFor(nameof(IsSomedaySelected))]
    private WishPriority _newPriority = WishPriority.Want;

    public bool IsWantSelected => NewPriority == WishPriority.Want;
    public bool IsNeedSelected => NewPriority == WishPriority.Need;
    public bool IsSomedaySelected => NewPriority == WishPriority.Someday;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewFoodSelected))]
    [NotifyPropertyChangedFor(nameof(IsNewShoppingSelected))]
    [NotifyPropertyChangedFor(nameof(IsNewHealthSelected))]
    [NotifyPropertyChangedFor(nameof(IsNewFunSelected))]
    [NotifyPropertyChangedFor(nameof(IsNewOtherSelected))]
    private ExpenseCategory _newCategory = ExpenseCategory.Shopping;

    public bool IsNewFoodSelected => NewCategory == ExpenseCategory.Food;
    public bool IsNewShoppingSelected => NewCategory == ExpenseCategory.Shopping;
    public bool IsNewHealthSelected => NewCategory == ExpenseCategory.Health;
    public bool IsNewFunSelected => NewCategory == ExpenseCategory.Fun;
    public bool IsNewOtherSelected => NewCategory == ExpenseCategory.Other;
    public bool NewHasPhoto => !string.IsNullOrEmpty(NewPhotoPath);

    // ── Detail modal ──────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isDetailVisible;
    [ObservableProperty] private WishCardItem? _selectedItem;
    [ObservableProperty] private AffordResult? _affordResult;
    [ObservableProperty] private bool _showAffordResult;

    // ── Filter commands ───────────────────────────────────────────────────────

    [RelayCommand] private void FilterAll() { ActiveFilter = WishFilter.All; ApplyFilter(); }
    [RelayCommand] private void FilterPlanned() { ActiveFilter = WishFilter.Planned; ApplyFilter(); }
    [RelayCommand] private void FilterBought() { ActiveFilter = WishFilter.Bought; ApplyFilter(); }
    [RelayCommand] private void FilterWant() { ActiveFilter = WishFilter.Want; ApplyFilter(); }
    [RelayCommand] private void FilterNeed() { ActiveFilter = WishFilter.Need; ApplyFilter(); }
    [RelayCommand] private void FilterSomeday() { ActiveFilter = WishFilter.Someday; ApplyFilter(); }

    // ── Add modal commands ────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddModal()
    {
        NewName = string.Empty;
        NewPrice = string.Empty;
        NewCaption = string.Empty;
        NewTargetMonth = string.Empty;
        NewPhotoPath = null;
        NewPriority = WishPriority.Want;
        NewCategory = ExpenseCategory.Shopping;
        OnPropertyChanged(nameof(NewHasPhoto));
        OnPropertyChanged(nameof(PriceLabelText));
        IsAddModalVisible = true;
    }

    [RelayCommand] private void DismissAddModal() => IsAddModalVisible = false;

    [RelayCommand]
    private void SelectPriority(string priority)
    {
        if (Enum.TryParse<WishPriority>(priority, out var p)) NewPriority = p;
    }

    [RelayCommand]
    private void SelectWishCategory(string category)
    {
        if (Enum.TryParse<ExpenseCategory>(category, out var cat)) NewCategory = cat;
    }

    [RelayCommand]
    private async Task PickWishPhotoAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result is null) return;
            var localPath = Path.Combine(FileSystem.AppDataDirectory, result.FileName);
            using var stream = await result.OpenReadAsync();
            using var fileStream = File.OpenWrite(localPath);
            await stream.CopyToAsync(fileStream);
            NewPhotoPath = localPath;
            OnPropertyChanged(nameof(NewHasPhoto));
        }
        catch { }
    }

    [RelayCommand]
    private void RemoveWishPhoto()
    {
        NewPhotoPath = null;
        OnPropertyChanged(nameof(NewHasPhoto));
    }

    [RelayCommand]
    private async Task SaveWishItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return;

        if (await _wishService.IsDuplicateAsync(NewName))
        {
            await Shell.Current.DisplayAlertAsync(
                "Already on your board",
                $"\"{NewName}\" is already in your Dream Board.", "OK");
            return;
        }

        decimal.TryParse(NewPrice, out var price);

        await _wishService.SaveWishItemAsync(new WishItemEntity
        {
            Name = NewName.Trim(),
            Price = price,
            Priority = NewPriority.ToString(),
            Category = NewCategory.ToString(),
            Caption = string.IsNullOrWhiteSpace(NewCaption) ? null : NewCaption.Trim(),
            PhotoPath = NewPhotoPath,
            TargetMonth = string.IsNullOrWhiteSpace(NewTargetMonth) ? null : NewTargetMonth,
            Status = WishStatus.Planned.ToString(),
            CreatedAt = DateTime.Now
        });

        IsAddModalVisible = false;
        await LoadAsync();
    }

    // ── Detail commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenDetailAsync(WishCardItem item)
    {
        SelectedItem = item;
        ShowAffordResult = false;
        AffordResult = null;
        IsDetailVisible = true;
        await CheckAffordabilityAsync(item);
    }

    [RelayCommand] private void DismissDetail() => IsDetailVisible = false;

    [RelayCommand]
    private async Task ToggleStatusAsync(string statusStr)
    {
        if (SelectedItem is null) return;
        if (!Enum.TryParse<WishStatus>(statusStr, out var status)) return;

        await _wishService.UpdateStatusAsync(SelectedItem.Id, status);
        SelectedItem.Status = status;
        var match = _allItems.FirstOrDefault(i => i.Id == SelectedItem.Id);
        if (match is not null) match.Status = status;
        UpdateStats();
    }

    [RelayCommand]
    private async Task SetRegretAsync(string rating)
    {
        if (SelectedItem is null) return;
        await _wishService.SetRegretAsync(SelectedItem.Id, rating);
        SelectedItem.RegretRating = rating;
        var match = _allItems.FirstOrDefault(i => i.Id == SelectedItem.Id);
        if (match is not null) match.RegretRating = rating;
    }

    [RelayCommand]
    private async Task PinItemAsync()
    {
        if (SelectedItem is null) return;
        await _wishService.PinItemAsync(SelectedItem.Id);
        IsDetailVisible = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ConvertToExpenseAsync()
    {
        if (SelectedItem is null) return;
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Move to Expenses",
            $"Add \"{SelectedItem.Name}\" to your expense feed?",
            "Move", "Cancel");
        if (!confirmed) return;

        await _wishService.ConvertToExpenseAsync(SelectedItem.Id);
        IsDetailVisible = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteWishItemAsync()
    {
        if (SelectedItem is null) return;
        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Remove",
            $"Remove \"{SelectedItem.Name}\" from your Dream Board?",
            "Remove", "Cancel");
        if (!confirmed) return;

        await _wishService.DeleteWishItemAsync(SelectedItem.Id);
        IsDetailVisible = false;
        await LoadAsync();
    }

    // ── Affordability ─────────────────────────────────────────────────────────

    private async Task CheckAffordabilityAsync(WishCardItem item)
    {
        var now = DateTime.Now;
        var budgets = await _budgetService.GetBudgetItemsAsync(now.Year, now.Month);
        var budget = budgets.FirstOrDefault(b => b.Category == item.Category);

        if (budget is null || budget.Limit <= 0) { ShowAffordResult = false; return; }

        var remaining = budget.Limit - budget.Spent;
        var canAfford = remaining >= item.Price;
        var sym = _settings.CurrencySymbol;

        AffordResult = new AffordResult
        {
            CanAfford = canAfford,
            CurrencySymbol = sym,
            BudgetRemaining = remaining,
            Difference = remaining - item.Price,
            CategoryName = item.CategoryLabel           
        };

        ShowAffordResult = true;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task OnPageAppearingAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _wishService.GetWishItemsAsync();
            _allItems = items.ToList();
            UpdateStats();
            ApplyFilter();
        }
        finally { IsLoading = false; }
    }

    private void ApplyFilter()
    {
        var filtered = ActiveFilter switch
        {
            WishFilter.Planned => _allItems.Where(i => i.Status == WishStatus.Planned),
            WishFilter.Bought => _allItems.Where(i => i.Status == WishStatus.Bought),
            WishFilter.Want => _allItems.Where(i => i.Priority == WishPriority.Want),
            WishFilter.Need => _allItems.Where(i => i.Priority == WishPriority.Need),
            WishFilter.Someday => _allItems.Where(i => i.Priority == WishPriority.Someday),
            _ => _allItems.AsEnumerable()
        };

        DisplayedItems = new ObservableCollection<WishCardItem>(filtered);
        OnPropertyChanged(nameof(HasNoItems));
        DataLoaded?.Invoke();
    }

    public bool HasNoItems => DisplayedItems.Count == 0;

    private void UpdateStats()
    {
        var planned = _allItems.Count(i => i.Status == WishStatus.Planned);
        var bought = _allItems.Count(i => i.Status == WishStatus.Bought);
        StatsLabel = $"{planned} planned · {bought} bought";
    }
}