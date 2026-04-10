using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;
using Tally_AestheticBudget.Views;

namespace Tally_AestheticBudget.ViewModels;

public partial class FeedViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;

    public FeedViewModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
        PickerYear = DateTime.Now.Year;
        _selectedPickerMonth = DateTime.Now.Month;
        BuildMonthOptions();
        _ = LoadFeedAsync();
    }

    // ── Feed items ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<FeedCardItem> _feedItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoEntries))]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _summaryLabel = string.Empty;

    [ObservableProperty]
    private string _totalSpentFormatted = "₱0.00";

    public string ThisYearLabel => $"This Year ({DateTime.Now.Year})";
    public bool HasNoEntries => !IsLoading && FeedItems.Count == 0;

    // ── Filter state ──────────────────────────────────────────────────────────

    private FilterMode _activeFilter = FilterMode.All;

    public bool IsFilterAll => _activeFilter == FilterMode.All;
    public bool IsFilterYear => _activeFilter == FilterMode.Year;
    public bool IsFilterMonth => _activeFilter == FilterMode.Month;

    private void SetFilter(FilterMode mode)
    {
        _activeFilter = mode;
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterYear));
        OnPropertyChanged(nameof(IsFilterMonth));
    }

    // ── Month picker ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isMonthPickerVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
    private int _pickerYear;

    private int _selectedPickerMonth;

    [ObservableProperty]
    private ObservableCollection<MonthOption> _monthOptions = [];

    public string SelectedMonthLabel => _activeFilter == FilterMode.Month
        ? new DateTime(PickerYear, _selectedPickerMonth, 1).ToString("MMM yyyy")
        : "Month";

    private void BuildMonthOptions()
    {
        MonthOptions.Clear();
        for (int m = 1; m <= 12; m++)
        {
            MonthOptions.Add(new MonthOption
            {
                Month = m,
                ShortName = new DateTime(2000, m, 1).ToString("MMM"),
                IsSelected = m == _selectedPickerMonth && _activeFilter == FilterMode.Month
            });
        }
    }

    // ── Detail modal ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isDetailVisible;

    [ObservableProperty]
    private FeedCardItem? _selectedItem;

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task GoToAddExpenseAsync()
    {
        await Shell.Current.GoToAsync(nameof(AddExpensePage));
    }

    // Passes the expense Id as a query parameter so AddExpenseViewModel
    // knows it's in edit mode and which expense to load
    [RelayCommand]
    private async Task GoToEditExpenseAsync(FeedCardItem item)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(AddExpensePage)}?ExpenseId={item.Id}");
    }

    public async Task OnPageAppearingAsync()
    {
        await LoadFeedAsync();
    }

    // ── Filter commands ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task FilterAllAsync()
    {
        SetFilter(FilterMode.All);
        await LoadFeedAsync();
    }

    [RelayCommand]
    private async Task FilterYearAsync()
    {
        SetFilter(FilterMode.Year);
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

    [RelayCommand]
    private void DismissDetail() => IsDetailVisible = false;

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
            IsDetailVisible = false;
            await LoadFeedAsync();
        }
        else
        {
            SelectedItem.RecalculateFromGroceryItems();
        }
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task LoadFeedAsync()
    {
        IsLoading = true;
        try
        {
            var items = _activeFilter switch
            {
                FilterMode.Year => await _expenseService.GetFeedItemsForYearAsync(PickerYear),
                FilterMode.Month => await _expenseService.GetFeedItemsForMonthAsync(PickerYear, _selectedPickerMonth),
                _ => await _expenseService.GetAllFeedItemsAsync()
            };

            FeedItems = new ObservableCollection<FeedCardItem>(items);
            UpdateLabels();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoEntries));
        }
    }

    private void UpdateLabels()
    {
        var total = FeedItems.Sum(x => x.Amount);
        TotalSpentFormatted = $"₱{total:N2}";
        SummaryLabel = _activeFilter switch
        {
            FilterMode.Month => new DateTime(PickerYear, _selectedPickerMonth, 1).ToString("MMMM yyyy"),
            FilterMode.Year => PickerYear.ToString(),
            _ => DateTime.Now.ToString("MMMM yyyy")
        };
    }
}

public enum FilterMode { All, Year, Month }