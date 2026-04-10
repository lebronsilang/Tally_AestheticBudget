using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class BudgetViewModel : ObservableObject
{
    private readonly IBudgetService _budgetService;

    public BudgetViewModel(IBudgetService budgetService)
    {
        _budgetService = budgetService;

        // Default to current month
        _currentYear = DateTime.Now.Year;
        _currentMonth = DateTime.Now.Month;

        _ = LoadBudgetAsync();
    }

    // ── State ─────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<BudgetCategoryItem> _budgetItems = [];

    [ObservableProperty]
    private bool _isLoading;

    // ── Month navigation ──────────────────────────────────────────────────────

    private int _currentYear;
    private int _currentMonth;

    [ObservableProperty]
    private string _monthLabel = string.Empty;

    // Total spent across all categories this month
    [ObservableProperty]
    private string _totalSpentLabel = string.Empty;

    // ── Commands — month navigation ───────────────────────────────────────────

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        _currentMonth--;
        if (_currentMonth < 1)
        {
            _currentMonth = 12;
            _currentYear--;
        }
        await LoadBudgetAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        _currentMonth++;
        if (_currentMonth > 12)
        {
            _currentMonth = 1;
            _currentYear++;
        }
        await LoadBudgetAsync();
    }

    // ── Commands — inline limit edit ──────────────────────────────────────────

    // Opens the inline edit row for a category
    [RelayCommand]
    private void StartEdit(BudgetCategoryItem item)
    {
        // Close any other open edit rows first
        foreach (var other in BudgetItems)
            other.IsEditing = false;

        item.EditLimitText = item.Limit > 0 ? item.Limit.ToString("N2") : string.Empty;
        item.IsEditing = true;
    }

    // Saves the new limit and closes the edit row
    [RelayCommand]
    private async Task SaveLimitAsync(BudgetCategoryItem item)
    {
        if (!decimal.TryParse(item.EditLimitText, out var newLimit) || newLimit < 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Invalid amount", "Please enter a valid number.", "OK");
            return;
        }

        await _budgetService.SetLimitAsync(
            _currentYear, _currentMonth, item.Category, newLimit);

        // Update in-memory so UI refreshes instantly without a full reload
        item.Limit = newLimit;
        item.IsEditing = false;

        UpdateTotalLabel();
    }

    // Cancels the edit without saving
    [RelayCommand]
    private void CancelEdit(BudgetCategoryItem item)
    {
        item.IsEditing = false;
        item.EditLimitText = string.Empty;
    }

    // Called by the page's OnAppearing
    public async Task OnPageAppearingAsync()
    {
        await LoadBudgetAsync();
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task LoadBudgetAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _budgetService.GetBudgetItemsAsync(_currentYear, _currentMonth);
            BudgetItems = new ObservableCollection<BudgetCategoryItem>(items);

            MonthLabel = new DateTime(_currentYear, _currentMonth, 1)
                .ToString("MMMM yyyy");

            UpdateTotalLabel();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateTotalLabel()
    {
        var total = BudgetItems.Sum(i => i.Spent);
        TotalSpentLabel = $"₱{total:N2}";
    }
}