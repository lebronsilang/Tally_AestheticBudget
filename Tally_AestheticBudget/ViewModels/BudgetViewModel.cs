using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class BudgetViewModel : ObservableObject
{
    private readonly IBudgetService _budgetService;
    private readonly ISettingsService _settings;

    public BudgetViewModel(IBudgetService budgetService, ISettingsService settings)
    {
        _budgetService = budgetService;
        _settings = settings;
        _currentYear = DateTime.Now.Year;
        _currentMonth = DateTime.Now.Month;

    }
    public string LimitPlaceholder => $"New limit ({_settings.CurrencySymbol})";

    [ObservableProperty] private ObservableCollection<BudgetCategoryItem> _budgetItems = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _monthLabel = string.Empty;
    [ObservableProperty] private string _totalSpentLabel = string.Empty;

    private int _currentYear;
    private int _currentMonth;

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        _currentMonth--;
        if (_currentMonth < 1) { _currentMonth = 12; _currentYear--; }
        await LoadBudgetAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        _currentMonth++;
        if (_currentMonth > 12) { _currentMonth = 1; _currentYear++; }
        await LoadBudgetAsync();
    }

    [RelayCommand]
    private void StartEdit(BudgetCategoryItem item)
    {
        foreach (var other in BudgetItems) other.IsEditing = false;
        item.EditLimitText = item.Limit > 0 ? item.Limit.ToString("N2") : string.Empty;
        item.IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveLimitAsync(BudgetCategoryItem item)
    {
        if (!decimal.TryParse(item.EditLimitText, out var newLimit) || newLimit < 0)
        {
            await Shell.Current.DisplayAlertAsync("Invalid amount",
                "Please enter a valid number.", "OK");
            return;
        }
        await _budgetService.SetLimitAsync(_currentYear, _currentMonth, item.Category, newLimit);
        item.Limit = newLimit;
        item.IsEditing = false;
        UpdateTotalLabel();
    }

    [RelayCommand]
    private void CancelEdit(BudgetCategoryItem item)
    {
        item.IsEditing = false;
        item.EditLimitText = string.Empty;
    }

    public async Task OnPageAppearingAsync() => await LoadBudgetAsync();

    private async Task LoadBudgetAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _budgetService.GetBudgetItemsAsync(_currentYear, _currentMonth);
            BudgetItems = new ObservableCollection<BudgetCategoryItem>(items);
            MonthLabel = new DateTime(_currentYear, _currentMonth, 1).ToString("MMMM yyyy");
            UpdateTotalLabel();
        }
        finally { IsLoading = false; }
    }

    private void UpdateTotalLabel()
    {
        var symbol = _settings.CurrencySymbol;
        var total = BudgetItems.Sum(i => i.Spent);
        TotalSpentLabel = $"{symbol}{total:N2}";
    }
}