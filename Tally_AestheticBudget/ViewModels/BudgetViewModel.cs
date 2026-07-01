using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using Tally_AestheticBudget.Controls;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;
using static Tally_AestheticBudget.Models.BudgetCategoryItem;

namespace Tally_AestheticBudget.ViewModels;

public partial class BudgetViewModel : ObservableObject
{
    private readonly IBudgetService _budgetService;
    private readonly IExpenseService _expenseService;
    private readonly ISettingsService _settings;
    private readonly DataChangedService _dataChanged;
    private readonly IThemeService _themeService;
    private readonly HeaderState _header;
    private bool _themeSubscribed;
    private bool _isDirty = true;
    private decimal _lastTotalLimit;   // captured each load for the editor prefill

    public BudgetViewModel(IBudgetService budgetService, IExpenseService expenseService,
        ISettingsService settings, DataChangedService dataChanged,
        IThemeService themeService, HeaderState header)
    {
        _budgetService = budgetService;
        _expenseService = expenseService;
        _settings = settings;
        _dataChanged = dataChanged;
        _themeService = themeService;
        _header = header;

        _pickerYear = DateTime.Now.Year;
        _selectedMonth = DateTime.Now.Month;
        _pickerFilterYear = DateTime.Now.Year;
        BuildMonthOptions();

        // BudgetChanged also fires when expenses/grocery change, so this one
        // subscription covers spent-total staleness too.
        _dataChanged.BudgetChanged += () => _isDirty = true;

        // SettingsChanged: notify XAML that ShowBudgetDonut may have flipped so the
        // IsVisible bindings on the list / donut layouts update immediately.
        _dataChanged.SettingsChanged += () =>
        {
            OnPropertyChanged(nameof(ShowBudgetDonut));
            OnPropertyChanged(nameof(ShowWideDonut));
            OnPropertyChanged(nameof(ShowNarrowDonut));
        };
    }

    public string LimitPlaceholder => $"New limit ({_settings.CurrencySymbol})";
    public string TotalPlaceholder => $"Monthly budget ({_settings.CurrencySymbol})";

    // ── Donut chart ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The IDrawable instance owned by this ViewModel.  Code-behind binds
    /// GraphicsView.Drawable to this and subscribes to its Invalidated event.
    /// </summary>
    public DonutDrawable DonutDrawable { get; } = new();

    /// <summary>
    /// Whether the budget-donut layout should be active.
    /// Reads directly from ISettingsService so a toggle in Settings is reflected
    /// immediately when SettingsChanged fires and raises this property.
    /// </summary>
    public bool ShowBudgetDonut => _settings.ShowBudgetDonut;

    /// <summary>
    /// Set by BudgetPage.OnSizeAllocated when the window is narrower than the
    /// side-by-side threshold. Switches the donut to a stacked (above-list) layout.
    /// </summary>
    public bool IsNarrowBudgetLayout
    {
        get => _isNarrowBudgetLayout;
        set
        {
            if (_isNarrowBudgetLayout == value) return;
            _isNarrowBudgetLayout = value;
            OnPropertyChanged(nameof(IsNarrowBudgetLayout));
            OnPropertyChanged(nameof(ShowWideDonut));
            OnPropertyChanged(nameof(ShowNarrowDonut));
        }
    }
    private bool _isNarrowBudgetLayout;

    /// <summary>True when the donut should render in the wide side-by-side column.</summary>
    public bool ShowWideDonut => ShowBudgetDonut && !IsNarrowBudgetLayout;
    /// <summary>True when the donut should render stacked above the category list.</summary>
    public bool ShowNarrowDonut => ShowBudgetDonut && IsNarrowBudgetLayout;

    /// <summary>
    /// Maps the current BudgetItems into DonutSegment records, excluding categories

    /// <summary>
    /// Maps the current BudgetItems into DonutSegment records, excluding categories
    /// with zero spending so the ring only shows meaningful slices.
    /// </summary>
    private List<DonutSegment> BuildDonutSegments() =>
        BudgetItems
            .Where(x => x.Spent > 0)
            .Select(x => new DonutSegment(x.DisplayName, x.Spent, x.Limit))
            .ToList();

    // ── Collection + period subtitle ─────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<BudgetCategoryItem> _budgetItems = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _periodLabel = string.Empty;

    // ── Total pill ───────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _hasTotal;
    [ObservableProperty] private bool _isOverTotal;
    [ObservableProperty] private bool _isPeriodEditable = true;
    [ObservableProperty] private double _totalProgress;
    [ObservableProperty] private string _totalLimitLabel = "Set budget";
    [ObservableProperty] private string _totalSpentLabel = string.Empty;
    [ObservableProperty] private string _totalRemainingLabel = string.Empty;

    // ── Filter state ─────────────────────────────────────────────────────────────

    private BudgetFilterMode _activeFilter = BudgetFilterMode.Month;
    public bool IsFilterDay => _activeFilter == BudgetFilterMode.Day;
    public bool IsFilterWeek => _activeFilter == BudgetFilterMode.Week;
    public bool IsFilterMonth => _activeFilter == BudgetFilterMode.Month;
    public bool IsFilterYear => _activeFilter == BudgetFilterMode.Year;

    private void SetFilter(BudgetFilterMode mode)
    {
        _activeFilter = mode;
        OnPropertyChanged(nameof(IsFilterDay));
        OnPropertyChanged(nameof(IsFilterWeek));
        OnPropertyChanged(nameof(IsFilterMonth));
        OnPropertyChanged(nameof(IsFilterYear));
    }

    // ── Day picker ───────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isDayPickerVisible;
    [ObservableProperty] private DateTime _pickerDay = DateTime.Today;
    public string SelectedDayLabel => _activeFilter == BudgetFilterMode.Day
        ? (_pickerDay.Date == DateTime.Today ? "Today" : _pickerDay.ToString("MMM d"))
        : "Today";

    // ── Week picker ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isWeekPickerVisible;
    [ObservableProperty] private ObservableCollection<WeekOption> _weekOptions = [];
    private DateTime _pickerWeekMonday = GetMondayOfCurrentWeek();
    public string SelectedWeekLabel => _activeFilter == BudgetFilterMode.Week
        ? (_pickerWeekMonday == GetMondayOfCurrentWeek() ? "This Week" : $"Wk of {_pickerWeekMonday:MMM d}")
        : "This Week";

    // ── Month picker ─────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    private int _pickerYear;
    private int _selectedMonth;
    [ObservableProperty] private bool _isMonthPickerVisible;
    [ObservableProperty] private ObservableCollection<MonthOption> _monthOptions = [];
    public string SelectedMonthLabel => _activeFilter == BudgetFilterMode.Month
        ? (_selectedMonth == DateTime.Now.Month && PickerYear == DateTime.Now.Year
            ? "This Month"
            : new DateTime(PickerYear, _selectedMonth, 1).ToString("MMM yyyy"))
        : "This Month";

    // ── Year picker ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isYearPickerVisible;
    private int _pickerFilterYear;
    [ObservableProperty] private List<YearOption> _yearList = [];
    public string SelectedYearLabel => _activeFilter == BudgetFilterMode.Year
        ? (_pickerFilterYear == DateTime.Now.Year ? $"This Year ({DateTime.Now.Year})" : _pickerFilterYear.ToString())
        : $"This Year ({DateTime.Now.Year})";

    public string FilterHeaderLabel => _activeFilter switch
    {
        BudgetFilterMode.Day => SelectedDayLabel,
        BudgetFilterMode.Week => SelectedWeekLabel,
        BudgetFilterMode.Year => SelectedYearLabel,
        _ => SelectedMonthLabel
    };

    // The Year field carries the filter-year in Year mode, the picker-year otherwise.
    private BudgetPeriod CurrentPeriod => new(
        _activeFilter,
        _pickerDay,
        _pickerWeekMonday,
        _activeFilter == BudgetFilterMode.Year ? _pickerFilterYear : PickerYear,
        _selectedMonth);

    // ── Lifecycle ────────────────────────────────────────────────────────────────

    public async Task OnPageAppearingAsync()
    {
        if (!_themeSubscribed)
        {
            _themeSubscribed = true;
            _themeService.ThemeChanged += OnThemeChanged;
        }
        // Clear any accent remnants left by a theme switch made on the Themes page.
        RefreshThemeBoundBindings();
        _header.ShowFilter(FilterHeaderLabel);
        var years = await _expenseService.GetDistinctExpenseYearsAsync();
        YearList = years.Select(y => new YearOption
        {
            Year = y,
            IsSelected = y == _pickerFilterYear && _activeFilter == BudgetFilterMode.Year
        }).ToList();
        if (!_isDirty) return;
        _isDirty = false;
        await LoadBudgetAsync();
    }

    public void OnPageDisappearing()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        _themeSubscribed = false;
        _themeService.RevertToGlobal();
    }

    private void OnThemeChanged() => RefreshThemeBoundBindings();

    /// <summary>Forces accent-dependent converters to re-evaluate. Called from the
    /// ThemeChanged event and on every appearance (covers theme switches made elsewhere).</summary>
    private void RefreshThemeBoundBindings()
    {
        // Filter chips
        OnPropertyChanged(nameof(IsFilterDay));
        OnPropertyChanged(nameof(IsFilterWeek));
        OnPropertyChanged(nameof(IsFilterMonth));
        OnPropertyChanged(nameof(IsFilterYear));

        // Total-budget progress bar — bound to BoolToProgressColor via IsOverTotal.
        OnPropertyChanged(nameof(IsOverTotal));

        // Category-level progress bars handled via RefreshThemeBindings() → IsOverLimit
        foreach (var item in BudgetItems) item.RefreshThemeBindings();

        // Month / week picker overlay cells (BoolToChipBg/Border/Text bound to IsSelected)
        foreach (var m in MonthOptions) m.RaiseThemeBindings();
        foreach (var w in WeekOptions) w.RaiseThemeBindings();
    }

    // ── Filter commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task FilterDayAsync()
    {
        _pickerDay = DateTime.Today;
        SetFilter(BudgetFilterMode.Day);
        OnPropertyChanged(nameof(SelectedDayLabel));
        await LoadBudgetAsync();
    }
    [RelayCommand] private void ShowDayPicker() => IsDayPickerVisible = true;
    [RelayCommand] private void DismissDayPicker() => IsDayPickerVisible = false;
    [RelayCommand]
    private async Task ConfirmDayPickerAsync()
    {
        IsDayPickerVisible = false;
        SetFilter(BudgetFilterMode.Day);
        OnPropertyChanged(nameof(SelectedDayLabel));
        await LoadBudgetAsync();
    }

    [RelayCommand]
    private async Task FilterWeekAsync()
    {
        _pickerWeekMonday = GetMondayOfCurrentWeek();
        SetFilter(BudgetFilterMode.Week);
        OnPropertyChanged(nameof(SelectedWeekLabel));
        await LoadBudgetAsync();
    }
    [RelayCommand]
    private void ShowWeekPicker()
    {
        WeekOptions.Clear();
        var thisMonday = GetMondayOfCurrentWeek();
        for (int i = 0; i < 8; i++)
        {
            var monday = thisMonday.AddDays(-7 * i);
            WeekOptions.Add(new WeekOption
            {
                Monday = monday,
                Label = i == 0 ? $"This week ({monday:MMM d})" : $"{monday:MMM d} – {monday.AddDays(6):MMM d}",
                IsSelected = monday == _pickerWeekMonday && _activeFilter == BudgetFilterMode.Week
            });
        }
        IsWeekPickerVisible = true;
    }
    [RelayCommand] private void DismissWeekPicker() => IsWeekPickerVisible = false;
    [RelayCommand]
    private async Task SelectWeekAsync(WeekOption option)
    {
        _pickerWeekMonday = option.Monday;
        IsWeekPickerVisible = false;
        SetFilter(BudgetFilterMode.Week);
        OnPropertyChanged(nameof(SelectedWeekLabel));
        await LoadBudgetAsync();
    }

    [RelayCommand]
    private async Task FilterMonthAsync()
    {
        _selectedMonth = DateTime.Now.Month;
        PickerYear = DateTime.Now.Year;
        SetFilter(BudgetFilterMode.Month);
        OnPropertyChanged(nameof(SelectedMonthLabel));
        BuildMonthOptions();
        await LoadBudgetAsync();
    }
    [RelayCommand]
    private void ShowMonthPicker()
    {
        BuildMonthOptions();
        IsMonthPickerVisible = true;
    }
    [RelayCommand] private void DismissMonthPicker() => IsMonthPickerVisible = false;
    [RelayCommand]
    private async Task SelectMonthAsync(MonthOption option)
    {
        _selectedMonth = option.Month;
        IsMonthPickerVisible = false;
        SetFilter(BudgetFilterMode.Month);
        OnPropertyChanged(nameof(SelectedMonthLabel));
        await LoadBudgetAsync();
    }
    [RelayCommand] private void PreviousYear() { PickerYear--; BuildMonthOptions(); }
    [RelayCommand] private void NextYear() { if (PickerYear < DateTime.Now.Year) { PickerYear++; BuildMonthOptions(); } }

    [RelayCommand]
    private async Task FilterYearAsync()
    {
        _pickerFilterYear = DateTime.Now.Year;
        SetFilter(BudgetFilterMode.Year);
        OnPropertyChanged(nameof(SelectedYearLabel));
        await LoadBudgetAsync();
    }
    [RelayCommand] private void ShowYearPicker() => IsYearPickerVisible = true;
    [RelayCommand] private void DismissYearPicker() => IsYearPickerVisible = false;
    [RelayCommand]
    private async Task SelectYearAsync(int year)
    {
        _pickerFilterYear = year;
        foreach (var opt in YearList) opt.IsSelected = opt.Year == year;
        IsYearPickerVisible = false;
        SetFilter(BudgetFilterMode.Year);
        OnPropertyChanged(nameof(SelectedYearLabel));
        await LoadBudgetAsync();
    }

    private void BuildMonthOptions()
    {
        if (MonthOptions.Count == 0)
            for (int m = 1; m <= 12; m++)
                MonthOptions.Add(new MonthOption { Month = m, ShortName = new DateTime(2000, m, 1).ToString("MMM") });
        foreach (var item in MonthOptions)
            item.IsSelected = item.Month == _selectedMonth && _activeFilter == BudgetFilterMode.Month;
    }

    // ── Total editor (Month view only) ───────────────────────────────────────────

    [ObservableProperty] private bool _isTotalEditVisible;
    [ObservableProperty] private string _editTotalText = string.Empty;

    [RelayCommand]
    private async Task ShowTotalEditorAsync()
    {
        if (!IsPeriodEditable)
        {
            await Shell.Current.DisplayAlertAsync("Monthly only",
                "Switch to This Month to set or change your budget.", "OK");
            return;
        }
        EditTotalText = _lastTotalLimit > 0 ? _lastTotalLimit.ToString("N2") : string.Empty;
        IsTotalEditVisible = true;
    }
    [RelayCommand] private void DismissTotalEditor() => IsTotalEditVisible = false;
    [RelayCommand]
    private async Task SaveTotalAsync()
    {
        if (!decimal.TryParse(EditTotalText, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) || v < 0)
        {
            await Shell.Current.DisplayAlertAsync("Invalid amount", "Please enter a valid number.", "OK");
            return;
        }
        await _budgetService.SetTotalAsync(PickerYear, _selectedMonth, v);
        IsTotalEditVisible = false;
        _dataChanged.NotifyBudgetChanged();
        await LoadBudgetAsync();
    }

    // ── Category limit editing (Month view only) ─────────────────────────────────

    [RelayCommand]
    private void StartEdit(BudgetCategoryItem item)
    {
        if (!item.CanEditLimit) return;
        foreach (var other in BudgetItems) other.IsEditing = false;

        item.EditIsUnlimited = item.Limit is null;
        item.EditLimitText = item.Limit is decimal v
            ? v.ToString("N2", CultureInfo.InvariantCulture)
            : string.Empty;
        item.IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveLimitAsync(BudgetCategoryItem item)
    {
        decimal? newLimit;

        if (item.EditIsUnlimited)
        {
            newLimit = null; // Unlimited
        }
        else
        {
            if (!decimal.TryParse(item.EditLimitText, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var v) || v < 0)
            {
                await Shell.Current.DisplayAlertAsync("Invalid amount",
                    "Enter a valid number, or switch on \u201CNo spending limit.\u201D", "OK");
                return;
            }
            newLimit = v; // 0 is allowed = an intentional ₱0 cap
        }

        await _budgetService.SetLimitAsync(PickerYear, _selectedMonth, item.Category, newLimit);
        item.IsEditing = false;
        _dataChanged.NotifyBudgetChanged();
        await LoadBudgetAsync();
    }

    [RelayCommand]
    private void CancelEdit(BudgetCategoryItem item)
    {
        item.IsEditing = false;
        item.EditLimitText = string.Empty;
        item.EditIsUnlimited = false;
    }

    // ── Load ─────────────────────────────────────────────────────────────────────

    private async Task LoadBudgetAsync()
    {
        IsLoading = true;
        try
        {
            var overview = await _budgetService.GetOverviewAsync(CurrentPeriod);
            BudgetItems = new ObservableCollection<BudgetCategoryItem>(overview.Items);
            ApplyOverview(overview);

            // Update donut chart data — must come after BudgetItems is set.
            DonutDrawable.CurrencySymbol = overview.CurrencySymbol;
            DonutDrawable.Segments = BuildDonutSegments();

            _header.ShowFilter(FilterHeaderLabel);
            await ApplyContextualThemeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadBudget failed: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Error",
                "Could not load budget data. Please try again.", "OK");
        }
        finally { IsLoading = false; }
    }

    private void ApplyOverview(BudgetOverview o)
    {
        var sym = o.CurrencySymbol;
        _lastTotalLimit = o.TotalLimit;
        HasTotal = o.HasTotal;
        IsOverTotal = o.IsOverTotal;
        IsPeriodEditable = o.IsEditable;
        TotalProgress = o.TotalLimit > 0 ? (double)Math.Min(o.TotalSpent / o.TotalLimit, 1m) : 0;
        TotalSpentLabel = $"{sym}{o.TotalSpent:N2}";
        TotalLimitLabel = o.HasTotal ? $"{sym}{o.TotalLimit:N2}" : "Set budget";
        TotalRemainingLabel = o.HasTotal
            ? (o.IsOverTotal
                ? $"{sym}{Math.Abs(o.TotalRemaining):N2} over"
                : $"{sym}{o.TotalRemaining:N2} left")
            : string.Empty;
        PeriodLabel = _activeFilter switch
        {
            BudgetFilterMode.Day => _pickerDay.ToString("dddd, MMM d"),
            BudgetFilterMode.Week => $"Week of {_pickerWeekMonday:MMM d}",
            BudgetFilterMode.Year => _pickerFilterYear.ToString(),
            _ => new DateTime(PickerYear, _selectedMonth, 1).ToString("MMMM yyyy")
        };
    }

    private async Task ApplyContextualThemeAsync()
    {
        if (_activeFilter != BudgetFilterMode.Month)
        {
            _themeService.RevertToGlobal();
            return;
        }
        try
        {
            var monthlyId = await _themeService.GetMonthlyThemeIdAsync(PickerYear, _selectedMonth);
            if (!string.IsNullOrEmpty(monthlyId)) _themeService.ApplyMonthlyPreview(monthlyId);
            else _themeService.RevertToGlobal();
        }
        catch { /* DB issue — stay on global */ }
    }

    private static DateTime GetMondayOfCurrentWeek()
    {
        var today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        return today.AddDays(-diff);
    }
}