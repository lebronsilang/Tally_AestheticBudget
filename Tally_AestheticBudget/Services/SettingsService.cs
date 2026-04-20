namespace Tally_AestheticBudget.Services;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface ISettingsService
{
    // Currency
    string CurrencySymbol { get; }
    string CurrencyCode { get; }
    string CurrencyName { get; }
    string CurrencyFlag { get; }
    void SetCurrency(string symbol, string code, string name, string flag);

    // Feed display toggles
    bool ShowNotes { get; set; }
    bool ShowPrice { get; set; }
    bool ShowDate { get; set; }

    // Wishlist toggles
    bool ShowCoolingOff { get; set; }
    bool ShowStaleReminder { get; set; }
}

// ── Implementation ────────────────────────────────────────────────────────────

public class SettingsService : ISettingsService
{
    private const string KeySymbol = "currency_symbol";
    private const string KeyCode = "currency_code";
    private const string KeyName = "currency_name";
    private const string KeyFlag = "currency_flag";
    private const string KeyShowNotes = "show_notes";
    private const string KeyShowPrice = "show_price";
    private const string KeyShowDate = "show_date";
    private const string KeyCoolingOff = "show_cooling_off";
    private const string KeyStale = "show_stale_reminder";

    public string CurrencySymbol => Preferences.Get(KeySymbol, "₱");
    public string CurrencyCode => Preferences.Get(KeyCode, "PHP");
    public string CurrencyName => Preferences.Get(KeyName, "Philippine Peso");
    public string CurrencyFlag => Preferences.Get(KeyFlag, "🇵🇭");

    public void SetCurrency(string symbol, string code, string name, string flag)
    {
        Preferences.Set(KeySymbol, symbol);
        Preferences.Set(KeyCode, code);
        Preferences.Set(KeyName, name);
        Preferences.Set(KeyFlag, flag);
    }

    public bool ShowNotes
    {
        get => Preferences.Get(KeyShowNotes, true);
        set => Preferences.Set(KeyShowNotes, value);
    }

    public bool ShowPrice
    {
        get => Preferences.Get(KeyShowPrice, true);
        set => Preferences.Set(KeyShowPrice, value);
    }

    public bool ShowDate
    {
        get => Preferences.Get(KeyShowDate, true);
        set => Preferences.Set(KeyShowDate, value);
    }

    public bool ShowCoolingOff
    {
        get => Preferences.Get(KeyCoolingOff, true);
        set => Preferences.Set(KeyCoolingOff, value);
    }

    public bool ShowStaleReminder
    {
        get => Preferences.Get(KeyStale, true);
        set => Preferences.Set(KeyStale, value);
    }
}