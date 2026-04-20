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
    public int CurrentColumnCount { get; set; } = 2;
    public FeedViewModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
        PickerYear = DateTime.Now.Year;
        _selectedPickerMonth = DateTime.Now.Month;
        BuildMonthOptions();
        _ = LoadFeedAsync();
    }

    // ── Feed items ────────────────────────────────────────────────────────────

    // Flat list — used for totals, delete commands, HasNoEntries
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoEntries))]
    private ObservableCollection<FeedCardItem> _feedItems = [];

    // Masonry columns — one ObservableCollection per column.
    // Code-behind reads this list and wires each column's BindableLayout.
    // ColumnsRebuilt event tells FeedPage.xaml.cs to rebuild the UI.
    private List<ObservableCollection<FeedCardItem>> _columns = [];
    public IReadOnlyList<ObservableCollection<FeedCardItem>> Columns => _columns;

    public event Action? ColumnsRebuilt;

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
        if (MonthOptions.Count == 0)
        {
            for (int m = 1; m <= 12; m++)
            {
                MonthOptions.Add(new MonthOption
                {
                    Month = m,
                    ShortName = new DateTime(2000, m, 1).ToString("MMM")
                });
            }
        }

        // just update selection state
        foreach (var item in MonthOptions)
        {
            item.IsSelected =
                item.Month == _selectedPickerMonth &&
                _activeFilter == FilterMode.Month;
        }
    }

    // ── Detail modal ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isDetailVisible;

    [ObservableProperty]
    private FeedCardItem? _selectedItem;

    // ── Add modal ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isAddModalVisible;

    [ObservableProperty]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    private string _newAmountText = string.Empty;

    [ObservableProperty]
    private string _newNote = string.Empty;

    [ObservableProperty]
    private DateTime _newSelectedDate = DateTime.Today;

    [ObservableProperty]
    private string? _newPhotoPath;

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
    private void RemoveNewPhoto()
    {
        NewPhotoPath = null;
        OnPropertyChanged(nameof(NewHasPhoto));
    }

    [RelayCommand]
    private async Task SaveNewExpenseAsync()
    {
        if (!decimal.TryParse(NewAmountText, out var amount) || amount <= 0) return;
        if (string.IsNullOrWhiteSpace(NewTitle)) return;

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
        await LoadFeedAsync();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

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

    [RelayCommand]
    private void DismissAddModal() => IsAddModalVisible = false;

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

    [RelayCommand]
    private void DismissEditModal() => IsEditModalVisible = false;

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
            var localPath = Path.Combine(FileSystem.AppDataDirectory, result.FileName);
            using var stream = await result.OpenReadAsync();
            using var fileStream = File.OpenWrite(localPath);
            await stream.CopyToAsync(fileStream);
            EditPhotoPath = localPath;
            OnPropertyChanged(nameof(EditHasPhoto));
        }
        catch { }
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
        if (!decimal.TryParse(EditAmountText, out var amount) || amount <= 0) return;
        if (string.IsNullOrWhiteSpace(EditTitle)) return;

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
        await LoadFeedAsync();
    }

    [RelayCommand]
    private async Task GoToEditExpenseAsync(FeedCardItem item)
    {
        // Load the existing expense into edit fields
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

    // Set to true by anything that changes expense data (save, delete).
    // OnPageAppearingAsync only reloads when dirty — prevents unnecessary
    // masonry rebuilds (and image enlarging) on simple navigation back.
    public bool IsDirty { get; set; } = true;  // true on first load

    public async Task OnPageAppearingAsync()
    {
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

        IsDirty = true;
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

            var itemList = items.ToList();
            FeedItems = new ObservableCollection<FeedCardItem>(itemList);

            if (itemList.Count == 0)
            {
                _columns = new List<ObservableCollection<FeedCardItem>>
                {
                    new(),
                    new()
                };

                OnPropertyChanged(nameof(Columns));
                ColumnsRebuilt?.Invoke();
                UpdateLabels();
                return;
            }

            DistributeIntoColumns(itemList, CurrentColumnCount);

            // DistributeIntoColumns uses the last known column count from code-behind.
            // Code-behind subscribes to ColumnsRebuilt and re-wires the UI.

            UpdateLabels();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoEntries));
        }
    }

    // Public so FeedPage.xaml.cs can call this on resize with a new columnCount.
    public void DistributeIntoColumns(IList<FeedCardItem>? itemList = null, int columnCount = 2)
    {
        var list = itemList ?? FeedItems.ToList();

        // Build fresh empty collections, one per column
        _columns.Clear();

        for (int i = 0; i < columnCount; i++)
            _columns.Add(new ObservableCollection<FeedCardItem>());

        OnPropertyChanged(nameof(Columns));
        ColumnsRebuilt?.Invoke();

        // Round-robin: item 0 → col 0, item 1 → col 1, item 2 → col 0, etc.
        for (int i = 0; i < list.Count; i++)
            _columns[i % columnCount].Add(list[i]);

        // Fire event — code-behind rebuilds the Grid columns from scratch
        ColumnsRebuilt?.Invoke();
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