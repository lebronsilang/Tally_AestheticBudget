using System.Globalization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class FeedViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;
    private readonly ISettingsService _settings;
    private readonly DataChangedService _dataChanged;
    private readonly IThemeService _themeService;
    private readonly HeaderState _header;
    private bool _themeSubscribed;

    public int CurrentColumnCount { get; set; } = 2;

    public FeedViewModel(IExpenseService expenseService, ISettingsService settings,
        DataChangedService dataChanged, IThemeService themeService, HeaderState header)
    {
        _expenseService = expenseService;
        _settings = settings;
        _dataChanged = dataChanged;
        _themeService = themeService;
        _header = header;
        PickerYear = DateTime.Now.Year;
        _selectedPickerMonth = DateTime.Now.Month;
        BuildMonthOptions();

        _dataChanged.ExpensesChanged += () => IsDirty = true;
        _dataChanged.SettingsChanged += () => IsDirty = true;
    }

    // ── Feed items ────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoEntries))]
    [NotifyPropertyChangedFor(nameof(HasEntries))]
    private ObservableCollection<FeedCardItem> _feedItems = [];

    private List<ObservableCollection<FeedCardItem>> _columns = [];
    public IReadOnlyList<ObservableCollection<FeedCardItem>> Columns => _columns;

    public event Action? ColumnsRebuilt;
    public event Action? FilterChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoEntries))]
    [NotifyPropertyChangedFor(nameof(HasEntries))]
    private bool _isLoading;

    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _summaryLabel = string.Empty;
    [ObservableProperty] private string _totalSpentFormatted = "0.00";

    public string ThisYearLabel => $"This Year ({DateTime.Now.Year})";

    // HasNoEntries → empty state overlay
    // HasEntries  → scroll view (header + masonry) — always visible so chips/add stay usable
    public bool HasNoEntries => !IsLoading && FeedItems.Count == 0;
    public bool HasEntries => true;

    // ── Filter state ──────────────────────────────────────────────────────────

    private FilterMode _activeFilter = FilterMode.All;
    public bool IsFilterAll => _activeFilter == FilterMode.All;
    public bool IsFilterDay => _activeFilter == FilterMode.Day;
    public bool IsFilterWeek => _activeFilter == FilterMode.Week;
    public bool IsFilterMonth => _activeFilter == FilterMode.Month;
    public bool IsFilterYear => _activeFilter == FilterMode.Year;
    public bool IsFilterCategory => _activeFilter == FilterMode.Category;

    private void SetFilter(FilterMode mode)
    {
        _activeFilter = mode;
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterDay));
        OnPropertyChanged(nameof(IsFilterWeek));
        OnPropertyChanged(nameof(IsFilterMonth));
        OnPropertyChanged(nameof(IsFilterYear));
        OnPropertyChanged(nameof(IsFilterCategory));
    }

    // ── Month picker ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    private int _pickerYear;

    public List<int> YearList { get; } = Enumerable.Range(DateTime.Now.Year - 5, 6).Reverse().ToList();

    private int _selectedPickerMonth;

    [ObservableProperty] private bool _isMonthPickerVisible;
    [ObservableProperty] private ObservableCollection<MonthOption> _monthOptions = [];

    public string SelectedMonthLabel => _activeFilter == FilterMode.Month
        ? (_selectedPickerMonth == DateTime.Now.Month && PickerYear == DateTime.Now.Year
            ? "This Month"
            : new DateTime(PickerYear, _selectedPickerMonth, 1).ToString("MMM yyyy"))
        : "This Month";

    private void BuildMonthOptions()
    {
        if (MonthOptions.Count == 0)
        {
            for (int m = 1; m <= 12; m++)
                MonthOptions.Add(new MonthOption
                {
                    Month = m,
                    ShortName = new DateTime(2000, m, 1).ToString("MMM")
                });
        }
        foreach (var item in MonthOptions)
            item.IsSelected = item.Month == _selectedPickerMonth && _activeFilter == FilterMode.Month;
    }

    // ── Category picker ───────────────────────────────────────────────────────

    private ExpenseCategory _selectedCategory = ExpenseCategory.Food;
    [ObservableProperty] private bool _isCategoryPickerVisible;

    // ── Day picker ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isDayPickerVisible;
    [ObservableProperty] private DateTime _pickerDay = DateTime.Today;

    public string SelectedDayLabel => _activeFilter == FilterMode.Day
        ? (_pickerDay.Date == DateTime.Today ? "Today" : _pickerDay.ToString("MMM d"))
        : "Today";

    // ── Week picker ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isWeekPickerVisible;
    [ObservableProperty] private ObservableCollection<WeekOption> _weekOptions = [];

    private DateTime _pickerWeekMonday = GetMondayOfCurrentWeek();

    public string SelectedWeekLabel => _activeFilter == FilterMode.Week
        ? (_pickerWeekMonday == GetMondayOfCurrentWeek()
            ? "This Week"
            : $"Wk of {_pickerWeekMonday:MMM d}")
        : "This Week";

    // ── Year picker ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isYearPickerVisible;

    private int _pickerFilterYear = DateTime.Now.Year;

    public string SelectedYearLabel => _activeFilter == FilterMode.Year
        ? (_pickerFilterYear == DateTime.Now.Year
            ? $"This Year ({DateTime.Now.Year})"
            : _pickerFilterYear.ToString())
        : $"This Year ({DateTime.Now.Year})";

    public string SelectedCategoryLabel => _activeFilter == FilterMode.Category
        ? _selectedCategory.ToString()
        : "Category";

    // ── Detail modal ──────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isDetailVisible;
    [ObservableProperty] private FeedCardItem? _selectedItem;

    // ── Add modal ─────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isAddModalVisible;
    [ObservableProperty] private string _newTitle = string.Empty;
    [ObservableProperty] private string _newAmountText = string.Empty;
    [ObservableProperty] private string _newNote = string.Empty;
    [ObservableProperty] private DateTime _newSelectedDate = DateTime.Today;
    [ObservableProperty] private string? _newPhotoPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewIsFoodSelected))]
    [NotifyPropertyChangedFor(nameof(NewIsTransportSelected))]
    [NotifyPropertyChangedFor(nameof(NewIsShoppingSelected))]
    [NotifyPropertyChangedFor(nameof(NewIsHealthSelected))]
    [NotifyPropertyChangedFor(nameof(NewIsFunSelected))]
    [NotifyPropertyChangedFor(nameof(NewIsOtherSelected))]
    private ExpenseCategory _newSelectedCategory = ExpenseCategory.Food;

    public bool NewIsFoodSelected => NewSelectedCategory == ExpenseCategory.Food;
    public bool NewIsTransportSelected => NewSelectedCategory == ExpenseCategory.Transport;
    public bool NewIsShoppingSelected => NewSelectedCategory == ExpenseCategory.Shopping;
    public bool NewIsHealthSelected => NewSelectedCategory == ExpenseCategory.Health;
    public bool NewIsFunSelected => NewSelectedCategory == ExpenseCategory.Fun;
    public bool NewIsOtherSelected => NewSelectedCategory == ExpenseCategory.Other;
    public bool NewHasPhoto => !string.IsNullOrEmpty(NewPhotoPath);

    [RelayCommand]
    private void SelectNewCategory(string cat)
    {
        if (Enum.TryParse<ExpenseCategory>(cat, out var c)) NewSelectedCategory = c;
    }

    [RelayCommand]
    private async Task PickNewPhotoAsync()
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
    private void RemoveNewPhoto()
    {
        NewPhotoPath = null;
        OnPropertyChanged(nameof(NewHasPhoto));
    }

    [RelayCommand]
    private async Task SaveNewExpenseAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTitle))
        {
            await Shell.Current.DisplayAlertAsync(
                "Title required",
                "Please give this expense a title before saving.",
                "OK");
            return;
        }

        if (!decimal.TryParse(NewAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Invalid amount",
                "Please enter a valid amount greater than zero.",
                "OK");
            return;
        }

        await _expenseService.SaveExpenseAsync(new ExpenseEntity
        {
            Title = NewTitle.Trim(),
            Amount = amount,
            Category = NewSelectedCategory.ToString(),
            Note = string.IsNullOrWhiteSpace(NewNote) ? null : NewNote.Trim(),
            Date = NewSelectedDate,
            PhotoPath = NewPhotoPath
        });

        IsAddModalVisible = false;
        IsDirty = true;
        _dataChanged.NotifyExpensesChanged();
        await LoadFeedAsync();
    }

    // ── Add modal open/close ──────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddModal()
    {
        NewTitle = string.Empty;
        NewAmountText = string.Empty;
        NewNote = string.Empty;
        NewSelectedDate = DateTime.Today;
        NewPhotoPath = null;
        NewSelectedCategory = ExpenseCategory.Food;
        OnPropertyChanged(nameof(NewHasPhoto));
        IsAddModalVisible = true;
    }

    [RelayCommand] private void DismissAddModal() => IsAddModalVisible = false;

    [RelayCommand]
    private async Task GoToAddExpenseAsync()
    {
        OpenAddModal();
        await Task.CompletedTask;
    }

    // ── Edit modal ────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isEditModalVisible;
    [ObservableProperty] private int _editExpenseId;
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string _editAmountText = string.Empty;
    [ObservableProperty] private string _editNote = string.Empty;
    [ObservableProperty] private DateTime _editSelectedDate = DateTime.Today;
    [ObservableProperty] private string? _editPhotoPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditIsFoodSelected))]
    [NotifyPropertyChangedFor(nameof(EditIsTransportSelected))]
    [NotifyPropertyChangedFor(nameof(EditIsShoppingSelected))]
    [NotifyPropertyChangedFor(nameof(EditIsHealthSelected))]
    [NotifyPropertyChangedFor(nameof(EditIsFunSelected))]
    [NotifyPropertyChangedFor(nameof(EditIsOtherSelected))]
    private ExpenseCategory _editSelectedCategory = ExpenseCategory.Food;

    public bool EditIsFoodSelected => EditSelectedCategory == ExpenseCategory.Food;
    public bool EditIsTransportSelected => EditSelectedCategory == ExpenseCategory.Transport;
    public bool EditIsShoppingSelected => EditSelectedCategory == ExpenseCategory.Shopping;
    public bool EditIsHealthSelected => EditSelectedCategory == ExpenseCategory.Health;
    public bool EditIsFunSelected => EditSelectedCategory == ExpenseCategory.Fun;
    public bool EditIsOtherSelected => EditSelectedCategory == ExpenseCategory.Other;
    public bool EditHasPhoto => !string.IsNullOrEmpty(EditPhotoPath);

    [RelayCommand] private void DismissEditModal() => IsEditModalVisible = false;

    [RelayCommand]
    private void SelectEditCategory(string cat)
    {
        if (Enum.TryParse<ExpenseCategory>(cat, out var c)) EditSelectedCategory = c;
    }

    [RelayCommand]
    private async Task PickEditPhotoAsync()
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
            EditPhotoPath = localPath;
            OnPropertyChanged(nameof(EditHasPhoto));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Photo pick failed: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Photo Error",
                "Could not load the selected photo. Please try again.", "OK");
        }
    }

    [RelayCommand]
    private void RemoveEditPhoto()
    {
        EditPhotoPath = null;
        OnPropertyChanged(nameof(EditHasPhoto));
    }

    [RelayCommand]
    private async Task SaveEditExpenseAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTitle))
        {
            await Shell.Current.DisplayAlertAsync(
                "Title required",
                "Please give this expense a title before saving.",
                "OK");
            return;
        }

        if (!decimal.TryParse(EditAmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Invalid amount",
                "Please enter a valid amount greater than zero.",
                "OK");
            return;
        }

        var expense = await _expenseService.GetExpenseByIdAsync(EditExpenseId);
        if (expense is null) return;

        expense.Title = EditTitle.Trim();
        expense.Amount = amount;
        expense.Category = EditSelectedCategory.ToString();
        expense.Note = string.IsNullOrWhiteSpace(EditNote) ? null : EditNote.Trim();
        expense.Date = EditSelectedDate;
        expense.PhotoPath = EditPhotoPath;

        await _expenseService.UpdateExpenseAsync(expense);
        IsEditModalVisible = false;
        IsDirty = true;
        _dataChanged.NotifyExpensesChanged();
        await LoadFeedAsync();
    }

    [RelayCommand]
    private async Task GoToEditExpenseAsync(FeedCardItem item)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(item.Id);
        if (expense is null) return;

        EditExpenseId = item.Id;
        EditTitle = expense.Title ?? string.Empty;
        EditAmountText = expense.Amount.ToString("N2");
        EditNote = expense.Note ?? string.Empty;
        EditSelectedDate = expense.Date;
        EditPhotoPath = expense.PhotoPath;
        EditSelectedCategory = Enum.TryParse<ExpenseCategory>(expense.Category, out var cat)
            ? cat : ExpenseCategory.Other;
        OnPropertyChanged(nameof(EditHasPhoto));

        IsDetailVisible = false;
        IsEditModalVisible = true;
    }

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
        await LoadFeedAsync();
    }

    // ── Filter commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task FilterAllAsync()
    {
        SetFilter(FilterMode.All);
        await LoadFeedAsync();
    }

    // ── Day filter ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task FilterDayAsync()
    {
        _pickerDay = DateTime.Today;
        SetFilter(FilterMode.Day);
        OnPropertyChanged(nameof(SelectedDayLabel));
        await LoadFeedAsync();
    }

    [RelayCommand]
    private void ShowDayPicker()
    {
        // Default to current value so DatePicker opens on right date
        IsDayPickerVisible = true;
    }

    [RelayCommand]
    private void DismissDayPicker() => IsDayPickerVisible = false;

    [RelayCommand]
    private async Task ConfirmDayPickerAsync()
    {
        IsDayPickerVisible = false;
        SetFilter(FilterMode.Day);
        OnPropertyChanged(nameof(SelectedDayLabel));
        await LoadFeedAsync();
    }

    // ── Week filter ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task FilterWeekAsync()
    {
        _pickerWeekMonday = GetMondayOfCurrentWeek();
        SetFilter(FilterMode.Week);
        OnPropertyChanged(nameof(SelectedWeekLabel));
        await LoadFeedAsync();
    }

    [RelayCommand]
    private void ShowWeekPicker()
    {
        // Build last 8 weeks of Mondays
        WeekOptions.Clear();
        var thisMonday = GetMondayOfCurrentWeek();
        for (int i = 0; i < 8; i++)
        {
            var monday = thisMonday.AddDays(-7 * i);
            WeekOptions.Add(new WeekOption
            {
                Monday = monday,
                Label = i == 0
                    ? $"This week ({monday:MMM d})"
                    : $"{monday:MMM d} – {monday.AddDays(6):MMM d}",
                IsSelected = monday == _pickerWeekMonday && _activeFilter == FilterMode.Week
            });
        }
        IsWeekPickerVisible = true;
    }

    [RelayCommand]
    private void DismissWeekPicker() => IsWeekPickerVisible = false;

    [RelayCommand]
    private async Task SelectWeekAsync(WeekOption option)
    {
        _pickerWeekMonday = option.Monday;
        IsWeekPickerVisible = false;
        SetFilter(FilterMode.Week);
        OnPropertyChanged(nameof(SelectedWeekLabel));
        await LoadFeedAsync();
    }

    // ── Month filter ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task FilterMonthAsync()
    {
        _selectedPickerMonth = DateTime.Now.Month;
        PickerYear = DateTime.Now.Year;
        SetFilter(FilterMode.Month);
        OnPropertyChanged(nameof(SelectedMonthLabel));
        BuildMonthOptions();
        await LoadFeedAsync();
    }

    [RelayCommand]
    private void ShowMonthPicker()
    {
        BuildMonthOptions();
        IsMonthPickerVisible = true;
    }

    [RelayCommand]
    private void DismissMonthPicker() => IsMonthPickerVisible = false;

    [RelayCommand]
    private async Task SelectMonthAsync(MonthOption option)
    {
        _selectedPickerMonth = option.Month;
        IsMonthPickerVisible = false;
        SetFilter(FilterMode.Month);
        OnPropertyChanged(nameof(SelectedMonthLabel));
        await LoadFeedAsync();
    }

    // ── Year filter ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task FilterYearAsync()
    {
        _pickerFilterYear = DateTime.Now.Year;
        SetFilter(FilterMode.Year);
        OnPropertyChanged(nameof(SelectedYearLabel));
        await LoadFeedAsync();
    }

    [RelayCommand]
    private void ShowYearPicker() => IsYearPickerVisible = true;

    [RelayCommand]
    private void DismissYearPicker() => IsYearPickerVisible = false;

    [RelayCommand]
    private async Task SelectYearAsync(int year)
    {
        _pickerFilterYear = year;
        IsYearPickerVisible = false;
        SetFilter(FilterMode.Year);
        OnPropertyChanged(nameof(SelectedYearLabel));
        await LoadFeedAsync();
    }

    // ── Category filter ─────────────────────────────────────────────────────

    [RelayCommand]
    private void ShowCategoryPicker() => IsCategoryPickerVisible = true;

    [RelayCommand]
    private void DismissCategoryPicker() => IsCategoryPickerVisible = false;

    [RelayCommand]
    private async Task SelectCategoryFilterAsync(string cat)
    {
        if (Enum.TryParse<ExpenseCategory>(cat, out var c))
            _selectedCategory = c;
        IsCategoryPickerVisible = false;
        SetFilter(FilterMode.Category);
        OnPropertyChanged(nameof(SelectedCategoryLabel));
        await LoadFeedAsync();
    }

    [RelayCommand]
    private void PreviousYear()
    {
        PickerYear--;
        BuildMonthOptions();
    }

    [RelayCommand]
    private void NextYear()
    {
        if (PickerYear < DateTime.Now.Year)
        {
            PickerYear++;
            BuildMonthOptions();
        }
    }

    // ── Feed commands ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadFeedAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private void OpenDetail(FeedCardItem item)
    {
        SelectedItem = item;
        IsDetailVisible = true;
    }

    [RelayCommand] private void DismissDetail() => IsDetailVisible = false;

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedItem is null) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Entry", "Are you sure you want to delete this?", "Delete", "Cancel");
        if (!confirmed) return;

        if (SelectedItem.IsGroceryGroup)
            await _expenseService.DeleteGroceryGroupAsync(SelectedItem.Id);
        else
            await _expenseService.DeleteExpenseAsync(SelectedItem.Id);

        IsDirty = true;
        _dataChanged.NotifyExpensesChanged();
        IsDetailVisible = false;
        await LoadFeedAsync();
    }

    [RelayCommand]
    private async Task DeleteAllGroceryItemsAsync()
    {
        if (SelectedItem is null) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete All", "Delete this entire grocery run?", "Delete All", "Cancel");
        if (!confirmed) return;

        await _expenseService.DeleteGroceryGroupAsync(SelectedItem.Id);
        IsDirty = true;
        _dataChanged.NotifyExpensesChanged();
        IsDetailVisible = false;
        await LoadFeedAsync();
    }

    [RelayCommand]
    private async Task DeleteGroceryItemAsync(GroceryLineItem item)
    {
        if (SelectedItem is null) return;

        await _expenseService.DeleteGroceryLineItemAsync(item.Id);
        SelectedItem.GroceryItems.Remove(item);

        if (SelectedItem.GroceryItems.Count == 0)
        {
            await _expenseService.DeleteGroceryGroupAsync(SelectedItem.Id);
            IsDirty = true;
            IsDetailVisible = false;
            await LoadFeedAsync();
        }
        else
        {
            SelectedItem.RecalculateFromGroceryItems();
        }
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private List<FeedCardItem> _pendingItems = [];

    private async Task ApplyContextualThemeAsync()
    {
        // Only apply contextual themes when filtering by Month
        if (_activeFilter != FilterMode.Month)
        {
            // Revert to global theme for non-month filters
            _themeService.RevertToGlobal();
            return;
        }

        try
        {
            var monthlyId = await _themeService.GetMonthlyThemeIdAsync(
                PickerYear, _selectedPickerMonth);

            if (!string.IsNullOrEmpty(monthlyId))
                _themeService.ApplyMonthlyPreview(monthlyId);
            else
                _themeService.RevertToGlobal();
        }
        catch
        {
            // DB issue — stay on global
        }
    }

    private async Task LoadFeedAsync()
    {
        FilterChanged?.Invoke();
        IsLoading = true;
        try
        {
            var items = _activeFilter switch
            {
                FilterMode.Year => await _expenseService.GetFeedItemsForYearAsync(_pickerFilterYear),
                FilterMode.Month => await _expenseService.GetFeedItemsForMonthAsync(PickerYear, _selectedPickerMonth),
                FilterMode.Day => await _expenseService.GetFeedItemsForDayAsync(_pickerDay),
                FilterMode.Week => await _expenseService.GetFeedItemsForWeekAsync(_pickerWeekMonday),
                FilterMode.Category => await _expenseService.GetFeedItemsByCategoryAsync(_selectedCategory),
                _ => await _expenseService.GetAllFeedItemsAsync()
            };

            var itemList = items.ToList();

            // Inject current settings into each card item
            var showNotes = _settings.ShowNotes;
            var showPrice = _settings.ShowPrice;
            var showDate = _settings.ShowDate;
            foreach (var item in itemList)
            {
                item.SettingShowNotes = showNotes;
                item.SettingShowPrice = showPrice;
                item.SettingShowDate = showDate;
            }

            FeedItems = new ObservableCollection<FeedCardItem>(itemList);
            _pendingItems = itemList;
            UpdateLabels();

            // Only distribute if we already have a real column count from OnSizeAllocated
            if (CurrentColumnCount > 0)
                DistributeIntoColumns(_pendingItems, CurrentColumnCount);
            await ApplyContextualThemeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadFeed failed: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Error",
                "Could not load expenses. Please try again.", "OK");
        }


        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoEntries));
            OnPropertyChanged(nameof(HasEntries));
        }
    }

    public void OnPageDisappearing()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _themeSubscribed = false;
        if (_activeFilter == FilterMode.Month)
            _themeService.RevertToGlobal();
    }

    private void OnThemeChanged()
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterDay));
        OnPropertyChanged(nameof(IsFilterWeek));
        OnPropertyChanged(nameof(IsFilterMonth));
        OnPropertyChanged(nameof(IsFilterYear));
        OnPropertyChanged(nameof(IsFilterCategory));
    }

    public string FilterHeaderLabel => _activeFilter switch
    {
        FilterMode.Day => SelectedDayLabel,
        FilterMode.Week => SelectedWeekLabel,
        FilterMode.Month => SelectedMonthLabel,
        FilterMode.Year => SelectedYearLabel,
        FilterMode.Category => SelectedCategoryLabel,
        _ => string.Empty          // empty = ShowBrand() in AppShell
    };

    // Public — FeedPage.xaml.cs calls this on resize
    public void DistributeIntoColumns(IList<FeedCardItem>? itemList = null, int columnCount = 2)
    {
        var list = itemList ?? FeedItems.ToList();
        bool isResize = _columns.Count != columnCount;

        if (isResize)
        {
            // Full rebuild — column count changed
            _columns = new List<ObservableCollection<FeedCardItem>>();
            for (int i = 0; i < columnCount; i++)
                _columns.Add(new ObservableCollection<FeedCardItem>());
            OnPropertyChanged(nameof(Columns));
        }
        else
        {
            // Soft refresh — reuse existing collections, just clear them
            foreach (var col in _columns)
                col.Clear();
        }

        var heights = new double[columnCount];

        foreach (var item in list)
        {
            int col = 0;
            for (int i = 1; i < columnCount; i++)
                if (heights[i] < heights[col]) col = i;

            _columns[col].Add(item);

            double h = item.HasPhoto ? 240 : 100;
            if (!string.IsNullOrEmpty(item.Title)) h += 20;
            if (!string.IsNullOrEmpty(item.Note)) h += 16;
            heights[col] += h + 10;
        }

        ColumnsRebuilt?.Invoke();
    }

    private void UpdateLabels()
    {
        var symbol = _settings.CurrencySymbol;
        var total = FeedItems.Sum(x => x.Amount);
        TotalSpentFormatted = $"{symbol}{total:N2}";
        SummaryLabel = _activeFilter switch
        {
            FilterMode.Month => new DateTime(PickerYear, _selectedPickerMonth, 1).ToString("MMMM yyyy"),
            FilterMode.Year => _pickerFilterYear.ToString(),
            FilterMode.Day => _pickerDay.ToString("dddd, MMM d"),
            FilterMode.Week => $"Week of {_pickerWeekMonday:MMM d}",
            FilterMode.Category => _selectedCategory.ToString(),
            _ => DateTime.Now.ToString("MMMM yyyy")
        };
        _header.ShowFilter(FilterHeaderLabel);
    }

    private static DateTime GetMondayOfCurrentWeek()
    {
        var today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        return today.AddDays(-diff);
    }
}

public enum FilterMode { All, Day, Week, Month, Year, Category }