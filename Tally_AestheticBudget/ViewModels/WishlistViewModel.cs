using System.Globalization;
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
    private readonly ISettingsService _settings;
    private readonly DataChangedService _dataChanged;
    private readonly IThemeService _themeService;
    private readonly HeaderState _header;
    private bool _themeSubscribed;

    private List<WishCardItem> _allItems = [];

    public int CurrentColumnCount { get; set; } = 2;

    public WishlistViewModel(
            IWishService wishService,
            IBudgetService budgetService,
            ISettingsService settings,
            DataChangedService dataChanged,
            IThemeService themeService,
            HeaderState header)
    {
        _wishService = wishService;
        _budgetService = budgetService;
        _settings = settings;
        _dataChanged = dataChanged;
        _themeService = themeService;
        _header = header;

        _dataChanged.WishlistChanged += () => IsDirty = true;
        _dataChanged.SettingsChanged += () =>
        {
            IsDirty = true;
            OnPropertyChanged(nameof(ExpensePanelOnLeft));
        };
    }

    public bool ExpensePanelOnLeft => _settings.ExpensePanelOnLeft;
    public string CurrencySymbol => _settings.CurrencySymbol;
    public string PriceLabelText => $"Price ({_settings.CurrencySymbol})";

    // View mode (masonry vs. list) — refreshed from settings on each appear.
    private bool _isListView;
    public bool IsListView
    {
        get => _isListView;
        private set
        {
            if (SetProperty(ref _isListView, value))
            {
                OnPropertyChanged(nameof(IsMasonryView));
                OnPropertyChanged(nameof(ShowMasonryArea));
                OnPropertyChanged(nameof(ShowListArea));
            }
        }
    }
    public bool IsMasonryView => !IsListView;
    public bool ShowMasonryArea => !HasNoItems && IsMasonryView;
    public bool ShowListArea => !HasNoItems && IsListView;

    // ── Items ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<WishCardItem> _displayedItems = [];

    public event Action? ColumnsRebuilt;
    public event Action? DataLoaded;
    public event Action? FilterChanged;

    // Change Columns type to match Feed's pattern
    private List<ObservableCollection<WishCardItem>> _columns = [];
    public IReadOnlyList<ObservableCollection<WishCardItem>> Columns => _columns;

    public void DistributeIntoColumns(int columnCount)
    {
        bool isResize = _columns.Count != columnCount;

        if (isResize)
        {
            _columns.Clear();

            for (int i = 0; i < columnCount; i++)
                _columns.Add(new ObservableCollection<WishCardItem>());

            OnPropertyChanged(nameof(Columns));
        }
        else
        {
            foreach (var col in _columns)
                col.Clear();
        }

        var heights = new double[columnCount];

        foreach (var item in DisplayedItems)
        {
            int col = 0;

            for (int i = 1; i < columnCount; i++)
                if (heights[i] < heights[col]) col = i;

            _columns[col].Add(item);

            double h = item.HasPhoto ? 240 : 100;

            if (!string.IsNullOrEmpty(item.Name)) h += 20;
            if (!string.IsNullOrEmpty(item.Caption)) h += 16;

            heights[col] += h + 10;
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

    private string FilterHeaderLabel => ActiveFilter switch
    {
        WishFilter.Planned => "Planned",
        WishFilter.Bought => "Bought",
        WishFilter.Want => "Want",
        WishFilter.Need => "Need",
        WishFilter.Someday => "Someday",
        _ => string.Empty
    };

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
            var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(result.FileName)}";
            var localPath = Path.Combine(FileSystem.AppDataDirectory, safeName);
            using var stream = await result.OpenReadAsync();
            using var fileStream = File.Create(localPath);
            await stream.CopyToAsync(fileStream);
            NewPhotoPath = localPath;
            OnPropertyChanged(nameof(NewHasPhoto));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Photo pick failed: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Photo Error",
                "Could not load the selected photo. Please try again.", "OK");
        }
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

        decimal.TryParse(NewPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price);

        // Validate TargetMonth format: must be "YYYY-MM" or empty.
        string? targetMonth = null;
        if (!string.IsNullOrWhiteSpace(NewTargetMonth))
        {
            var raw = NewTargetMonth.Trim();
            var parts = raw.Split('-');
            bool validFormat =
                parts.Length == 2
                && int.TryParse(parts[0], out var tmYear) && tmYear >= 2000 && tmYear <= 2100
                && int.TryParse(parts[1], out var tmMonth) && tmMonth >= 1 && tmMonth <= 12;

            if (!validFormat)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Invalid target month",
                    "Enter the target month as YYYY-MM (e.g. 2025-09).",
                    "OK");
                return;
            }
            targetMonth = raw;
        }

        await _wishService.SaveWishItemAsync(new WishItemEntity
        {
            Name = NewName.Trim(),
            Price = price,
            Priority = NewPriority.ToString(),
            Category = NewCategory.ToString(),
            Caption = string.IsNullOrWhiteSpace(NewCaption) ? null : NewCaption.Trim(),
            PhotoPath = NewPhotoPath,
            TargetMonth = targetMonth,
            Status = WishStatus.Planned.ToString(),
            CreatedAt = DateTime.Now
        });


        IsAddModalVisible = false;
        IsDirty = true;
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
        IsDirty = true;
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
        _dataChanged.NotifyExpensesChanged();
        IsDetailVisible = false;
        IsDirty = true;
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
        IsDirty = true;
        await LoadAsync();
    }

    // ── Affordability ─────────────────────────────────────────────────────────

    private async Task CheckAffordabilityAsync(WishCardItem item)
    {
        var now = DateTime.Now;
        var budgets = await _budgetService.GetBudgetItemsAsync(now.Year, now.Month);
        var budget = budgets.FirstOrDefault(b => b.Category == item.Category);

        if (budget is null || budget.Limit is not decimal lim || lim <= 0m)
        {
            ShowAffordResult = false;
            return;
        }
        var remaining = lim - budget.Spent;
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

    public bool IsDirty { get; set; } = true;

    public async Task OnPageAppearingAsync()
    {
        if (!_themeSubscribed)
        {
            _themeSubscribed = true;
            _themeService.ThemeChanged += OnThemeChanged;
        }
        IsListView = _settings.WishlistListView;
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
        OnPropertyChanged(nameof(IsFilterPlanned));
        OnPropertyChanged(nameof(IsFilterBought));
        OnPropertyChanged(nameof(IsFilterWant));
        OnPropertyChanged(nameof(IsFilterNeed));
        OnPropertyChanged(nameof(IsFilterSomeday));
        foreach (var item in DisplayedItems)
            item.RefreshThemeBindings();
    }

    private async Task LoadAsync()
    {
        FilterChanged?.Invoke();
        IsLoading = true;
        try
        {
            var items = await _wishService.GetWishItemsAsync();
            _allItems = items.ToList();

            // Inject current settings into each card item
            var showCooling = _settings.ShowCoolingOff;
            var showStale = _settings.ShowStaleReminder;
            var listShowPhoto = _settings.ListViewShowsPhoto;
            foreach (var item in _allItems)
            {
                item.SettingShowCooling = showCooling;
                item.SettingShowStale = showStale;
                item.SettingListShowPhoto = listShowPhoto;
            }

            UpdateStats();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadWishlist failed: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Error",
                "Could not load wishlist data. Please try again.", "OK");
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
        OnPropertyChanged(nameof(ShowMasonryArea));
        OnPropertyChanged(nameof(ShowListArea));

        // rebuild masonry columns automatically
        if (CurrentColumnCount > 0)
            DistributeIntoColumns(CurrentColumnCount);

        DataLoaded?.Invoke();
        _header.ShowFilter(FilterHeaderLabel);
    }

    public bool HasNoItems => DisplayedItems.Count == 0;

    private void UpdateStats()
    {
        var planned = _allItems.Count(i => i.Status == WishStatus.Planned);
        var bought = _allItems.Count(i => i.Status == WishStatus.Bought);
        StatsLabel = $"{planned} planned · {bought} bought";
    }
}