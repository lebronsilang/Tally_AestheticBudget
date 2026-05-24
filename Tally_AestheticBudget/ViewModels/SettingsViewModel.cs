using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class CurrencyOption(string Flag, string Name, string Code, string Symbol)
    : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Flag { get; init; } = Flag;
    public string Name { get; init; } = Name;
    public string Code { get; init; } = Code;
    public string Symbol { get; init; } = Symbol;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isSelected;
}


public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IExpenseService _expenses;
    private readonly IGroceryService _grocery;
    private readonly IWishService _wishes;

    public SettingsViewModel(
        ISettingsService settings,
        IExpenseService expenses,
        IGroceryService grocery,
        IWishService wishes)
    {
        _settings = settings;
        _expenses = expenses;
        _grocery = grocery;
        _wishes = wishes;

        _selectedCurrency = AllCurrencies.FirstOrDefault(
            c => c.Code == settings.CurrencyCode) ?? AllCurrencies[0];

        _showNotes = settings.ShowNotes;
        _showPrice = settings.ShowPrice;
        _showDate = settings.ShowDate;
        _showCoolingOff = settings.ShowCoolingOff;
        _showStaleReminder = settings.ShowStaleReminder;

        FilteredCurrencies = new ObservableCollection<CurrencyOption>(
            AllCurrencies.Select(c => { c.IsSelected = c.Code == settings.CurrencyCode; return c; }));
    }

    // ── Currency ──────────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<CurrencyOption> AllCurrencies =
    [
        new("🇵🇭", "Philippine Peso",    "PHP", "₱"),
        new("🇺🇸", "US Dollar",           "USD", "$"),
        new("🇪🇺", "Euro",                "EUR", "€"),
        new("🇬🇧", "British Pound",       "GBP", "£"),
        new("🇯🇵", "Japanese Yen",        "JPY", "¥"),
        new("🇰🇷", "Korean Won",          "KRW", "₩"),
        new("🇦🇺", "Australian Dollar",   "AUD", "A$"),
        new("🇨🇦", "Canadian Dollar",     "CAD", "C$"),
        new("🇨🇭", "Swiss Franc",         "CHF", "Fr"),
        new("🇨🇳", "Chinese Yuan",        "CNY", "¥"),
        new("🇮🇳", "Indian Rupee",        "INR", "₹"),
        new("🇮🇩", "Indonesian Rupiah",   "IDR", "Rp"),
        new("🇲🇾", "Malaysian Ringgit",   "MYR", "RM"),
        new("🇸🇬", "Singapore Dollar",    "SGD", "S$"),
        new("🇹🇭", "Thai Baht",           "THB", "฿"),
        new("🇻🇳", "Vietnamese Dong",     "VND", "₫"),
        new("🇧🇷", "Brazilian Real",      "BRL", "R$"),
        new("🇲🇽", "Mexican Peso",        "MXN", "$"),
        new("🇿🇦", "South African Rand",  "ZAR", "R"),
        new("🇳🇬", "Nigerian Naira",      "NGN", "₦"),
        new("🇦🇪", "UAE Dirham",          "AED", "د.إ"),
        new("🇸🇦", "Saudi Riyal",         "SAR", "﷼"),
        new("🇹🇷", "Turkish Lira",        "TRY", "₺"),
        new("🇷🇺", "Russian Ruble",       "RUB", "₽"),
        new("🇸🇪", "Swedish Krona",       "SEK", "kr"),
        new("🇳🇴", "Norwegian Krone",     "NOK", "kr"),
        new("🇩🇰", "Danish Krone",        "DKK", "kr"),
        new("🇵🇱", "Polish Zloty",        "PLN", "zł"),
        new("🇨🇿", "Czech Koruna",        "CZK", "Kč"),
        new("🇭🇺", "Hungarian Forint",    "HUF", "Ft"),
        new("🇷🇴", "Romanian Leu",        "RON", "lei"),
        new("🇮🇱", "Israeli Shekel",      "ILS", "₪"),
        new("🇪🇬", "Egyptian Pound",      "EGP", "£"),
        new("🇵🇰", "Pakistani Rupee",     "PKR", "₨"),
        new("🇧🇩", "Bangladeshi Taka",    "BDT", "৳"),
        new("🇳🇿", "New Zealand Dollar",  "NZD", "NZ$"),
        new("🇭🇰", "Hong Kong Dollar",    "HKD", "HK$"),
        new("🇹🇼", "Taiwan Dollar",       "TWD", "NT$"),
        new("🇦🇷", "Argentine Peso",      "ARS", "$"),
        new("🇨🇱", "Chilean Peso",        "CLP", "$"),
        new("🇨🇴", "Colombian Peso",      "COP", "$"),
        new("🇵🇪", "Peruvian Sol",        "PEN", "S/"),
        new("🇺🇦", "Ukrainian Hryvnia",   "UAH", "₴"),
        new("🇰🇿", "Kazakhstani Tenge",   "KZT", "₸"),
        new("🇬🇭", "Ghanaian Cedi",       "GHS", "₵"),
        new("🇰🇪", "Kenyan Shilling",     "KES", "KSh"),
        new("🇲🇦", "Moroccan Dirham",     "MAD", "MAD"),
        new("🇶🇦", "Qatari Riyal",        "QAR", "﷼"),
        new("🇰🇼", "Kuwaiti Dinar",       "KWD", "KD"),
        new("🇧🇭", "Bahraini Dinar",      "BHD", "BD"),
    ];

    public ObservableCollection<CurrencyOption> FilteredCurrencies { get; }

    [ObservableProperty]
    private CurrencyOption _selectedCurrency;

    [ObservableProperty]
    private string _currencySearch = string.Empty;

    partial void OnCurrencySearchChanged(string value)
    {
        FilteredCurrencies.Clear();
        var q = value.Trim().ToLowerInvariant();
        foreach (var c in AllCurrencies)
        {
            if (string.IsNullOrEmpty(q) ||
                c.Name.ToLower().Contains(q) ||
                c.Code.ToLower().Contains(q) ||
                c.Symbol.ToLower().Contains(q))
                FilteredCurrencies.Add(c);
        }
    }

    [RelayCommand]
    private void SelectCurrency(CurrencyOption currency)
    {
        SelectedCurrency = currency;
        _settings.SetCurrency(currency.Symbol, currency.Code, currency.Name, currency.Flag);

        // Update checkmarks
        foreach (var c in FilteredCurrencies)
            c.IsSelected = c.Code == currency.Code;

        // Force CollectionView to re-evaluate by refreshing the list
        var temp = FilteredCurrencies.ToList();
        FilteredCurrencies.Clear();
        foreach (var c in temp) FilteredCurrencies.Add(c);
    }

    // ── Feed toggles ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _showNotes;
    partial void OnShowNotesChanged(bool value) => _settings.ShowNotes = value;

    [ObservableProperty]
    private bool _showPrice;
    partial void OnShowPriceChanged(bool value) => _settings.ShowPrice = value;

    [ObservableProperty]
    private bool _showDate;
    partial void OnShowDateChanged(bool value) => _settings.ShowDate = value;

    // ── Wishlist toggles ──────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _showCoolingOff;
    partial void OnShowCoolingOffChanged(bool value) => _settings.ShowCoolingOff = value;

    [ObservableProperty]
    private bool _showStaleReminder;
    partial void OnShowStaleReminderChanged(bool value) => _settings.ShowStaleReminder = value;

    // ── Toggle commands — flip the bool properties ────────────────────────────

    [RelayCommand]
    private void ToggleShowNotes() => ShowNotes = !ShowNotes;

    [RelayCommand]
    private void ToggleShowPrice() => ShowPrice = !ShowPrice;

    [RelayCommand]
    private void ToggleShowDate() => ShowDate = !ShowDate;

    [RelayCommand]
    private void ToggleShowCoolingOff() => ShowCoolingOff = !ShowCoolingOff;

    [RelayCommand]
    private void ToggleShowStaleReminder() => ShowStaleReminder = !ShowStaleReminder;

    // ── Clear data ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isClearDataVisible;

    [RelayCommand]
    private void OpenClearData() => IsClearDataVisible = true;

    [RelayCommand]
    private void DismissClearData() => IsClearDataVisible = false;

    [RelayCommand]
    private async Task ClearExpenses()
    {
        await _expenses.DeleteAllAsync();
        IsClearDataVisible = false;
    }

    [RelayCommand]
    private async Task ClearGroceries()
    {
        await _grocery.DeleteAllAsync();
        IsClearDataVisible = false;
    }

    [RelayCommand]
    private async Task ClearWishlist()
    {
        await _wishes.DeleteAllAsync();
        IsClearDataVisible = false;
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        await _expenses.DeleteAllAsync();
        await _grocery.DeleteAllAsync();
        await _wishes.DeleteAllAsync();
        IsClearDataVisible = false;
    }
}