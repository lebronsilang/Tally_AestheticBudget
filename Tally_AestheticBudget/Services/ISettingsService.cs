namespace Tally_AestheticBudget.Services;

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