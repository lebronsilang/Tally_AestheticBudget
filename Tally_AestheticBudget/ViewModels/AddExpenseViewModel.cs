using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class AddExpenseViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;

    public AddExpenseViewModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
        SelectedDate = DateTime.Today;
    }

    // ── Form fields ───────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _amountText = string.Empty;

    // Short label shown on the feed card
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _title = string.Empty;

    // Longer optional detail shown only in the bottom sheet
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

    // ── Validation — both Amount and Title are required ───────────────────────

    private bool CanSave() =>
        decimal.TryParse(AmountText, out var v) && v > 0
        && !string.IsNullOrWhiteSpace(Title);

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
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choose a photo"
            });

            if (result is null) return;

            var localPath = Path.Combine(FileSystem.AppDataDirectory, result.FileName);

            using var stream = await result.OpenReadAsync();
            using var fileStream = File.OpenWrite(localPath);
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
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0) return;

        var expense = new ExpenseEntity
        {
            Amount = amount,
            Title = Title.Trim(),
            Category = SelectedCategory.ToString(),
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
            Date = SelectedDate,
            PhotoPath = PhotoPath
        };

        await _expenseService.SaveExpenseAsync(expense);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}