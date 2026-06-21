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
    bool AllotRemainingToUnallocated { get; set; }

    // View-mode toggles (masonry vs. list)
    bool FeedListView { get; set; }
    bool WishlistListView { get; set; }
    bool ListViewShowsPhoto { get; set; }
    bool ExpensePanelOnLeft { get; set; }

    // Chart toggles
    bool ShowBudgetDonut { get; set; }  // key: "show_budget_donut",  default: true
    bool ShowFeedBar { get; set; }   // key: "show_feed_bar",      default: true
    bool FeedBarSticky { get; set; }   // key: "feed_bar_sticky",    default: false

}

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
    private const string KeyAllotRemaining = "allot_remaining_unallocated";
    private const string KeyFeedListView = "feed_list_view";
    private const string KeyWishlistListView = "wishlist_list_view";
    private const string KeyListViewShowsPhoto = "list_view_shows_photo";

    private const string KeyShowBudgetDonut = "show_budget_donut";
    private const string KeyShowFeedBar = "show_feed_bar";
    private const string KeyFeedBarSticky = "feed_bar_sticky";


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

    public bool AllotRemainingToUnallocated
    {
        get => Preferences.Get("allot_remaining_to_unallocated", false);
        set => Preferences.Set("allot_remaining_to_unallocated", value);
    }

    public bool FeedListView
    {
        get => Preferences.Get(KeyFeedListView, false);
        set => Preferences.Set(KeyFeedListView, value);
    }

    public bool WishlistListView
    {
        get => Preferences.Get(KeyWishlistListView, false);
        set => Preferences.Set(KeyWishlistListView, value);
    }

    public bool ListViewShowsPhoto
    {
        get => Preferences.Get(KeyListViewShowsPhoto, true);
        set => Preferences.Set(KeyListViewShowsPhoto, value);
    }

    public bool ExpensePanelOnLeft
    {
        get => Preferences.Get("expense_panel_on_left", false);
        set => Preferences.Set("expense_panel_on_left", value);
    }

    public bool ShowBudgetDonut
    {
        get => Preferences.Get(KeyShowBudgetDonut, true);
        set => Preferences.Set(KeyShowBudgetDonut, value);
    }

    public bool ShowFeedBar
    {
        get => Preferences.Get(KeyShowFeedBar, false);
        set => Preferences.Set(KeyShowFeedBar, value);
    }

    public bool FeedBarSticky
    {
        get => Preferences.Get(KeyFeedBarSticky, false);
        set => Preferences.Set(KeyFeedBarSticky, value);
    }


}