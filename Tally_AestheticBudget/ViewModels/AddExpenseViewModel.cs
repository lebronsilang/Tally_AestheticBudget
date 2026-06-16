using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

// Shell passes query parameters as strings — this attribute lets
// AddExpenseViewModel receive the ExpenseId when navigating for edit.
[QueryProperty(nameof(ExpenseId), "ExpenseId")]
public partial class AddExpenseViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;
    private readonly ISettingsService _settings;

    public AddExpenseViewModel(IExpenseService expenseService, ISettingsService settings)
    {
        _expenseService = expenseService;
        _settings = settings;
        SelectedDate = DateTime.Today;
    }

    // ── Mode detection ────────────────────────────────────────────────────────

    // Set by Shell navigation when editing — 0 means Add mode
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    [NotifyPropertyChangedFor(nameof(SaveButtonLabel))]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private int _expenseId;

    public bool IsEditMode => ExpenseId > 0;
    public string PageTitle => IsEditMode ? "Edit Entry ✦" : "New Entry ✦";
    public string SaveButtonLabel => IsEditMode ? "Update Entry" : "Save Entry";

    // ── Form fields ───────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _amountText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private DateTime _selectedDate;

    [ObservableProperty]
    private string? _photoPath;

    // ── Category selection ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFoodSelected))]
    [NotifyPropertyChangedFor(nameof(IsTransportSelected))]
    [NotifyPropertyChangedFor(nameof(IsShoppingSelected))]
    [NotifyPropertyChangedFor(nameof(IsHealthSelected))]
    [NotifyPropertyChangedFor(nameof(IsFunSelected))]
    [NotifyPropertyChangedFor(nameof(IsOtherSelected))]
    private ExpenseCategory _selectedCategory = ExpenseCategory.Food;

    public bool IsFoodSelected => SelectedCategory == ExpenseCategory.Food;
    public bool IsTransportSelected => SelectedCategory == ExpenseCategory.Transport;
    public bool IsShoppingSelected => SelectedCategory == ExpenseCategory.Shopping;
    public bool IsHealthSelected => SelectedCategory == ExpenseCategory.Health;
    public bool IsFunSelected => SelectedCategory == ExpenseCategory.Fun;
    public bool IsOtherSelected => SelectedCategory == ExpenseCategory.Other;

    // ── Photo ─────────────────────────────────────────────────────────────────

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);

    // ── Display helpers ───────────────────────────────────────────────────────

    public string AmountLabel => $"Amount ({_settings.CurrencySymbol})";

    // ── Validation ────────────────────────────────────────────────────────────

    // InvariantCulture ensures "1234.56" always parses correctly regardless of the
    // device's regional number-format setting (e.g. locales that use "," as decimal).
    private bool CanSave() =>
        decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v > 0
        && !string.IsNullOrWhiteSpace(Title);

    // ── Load existing expense when in edit mode ───────────────────────────────

    public async Task LoadExistingAsync()
    {
        if (!IsEditMode) return;

        var expense = await _expenseService.GetExpenseByIdAsync(ExpenseId);
        if (expense is null) return;

        Title = expense.Title ?? string.Empty;
        AmountText = expense.Amount.ToString("N2", CultureInfo.InvariantCulture);
        Note = expense.Note ?? string.Empty;
        SelectedDate = expense.Date;
        PhotoPath = expense.PhotoPath;
        SelectedCategory = Enum.TryParse<ExpenseCategory>(expense.Category, out var cat)
            ? cat
            : ExpenseCategory.Other;

        OnPropertyChanged(nameof(HasPhoto));
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectCategory(string categoryName)
    {
        if (Enum.TryParse<ExpenseCategory>(categoryName, out var cat))
            SelectedCategory = cat;
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choose a photo"
            });

            if (result is null) return;

            var safeName = $"{Guid.NewGuid():N}{Path.GetExtension(result.FileName)}";
            var localPath = Path.Combine(FileSystem.AppDataDirectory, safeName);
            using var stream = await result.OpenReadAsync();
            using var fileStream = File.Create(localPath);
            await stream.CopyToAsync(fileStream);
            PhotoPath = localPath;
            OnPropertyChanged(nameof(HasPhoto));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not load photo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        PhotoPath = null;
        OnPropertyChanged(nameof(HasPhoto));
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        // CanSave already guards the button — this is a safety net in case Save is
        // somehow triggered programmatically with invalid state.
        if (!decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return;

        if (IsEditMode)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(ExpenseId);
            if (expense is null) return;

            expense.Title = Title.Trim();
            expense.Amount = amount;
            expense.Category = SelectedCategory.ToString();
            expense.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            expense.Date = SelectedDate;
            expense.PhotoPath = PhotoPath;

            await _expenseService.UpdateExpenseAsync(expense);
        }
        else
        {
            var expense = new ExpenseEntity
            {
                Title = Title.Trim(),
                Amount = amount,
                Category = SelectedCategory.ToString(),
                Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
                Date = SelectedDate,
                PhotoPath = PhotoPath
            };

            await _expenseService.SaveExpenseAsync(expense);
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}