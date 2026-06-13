using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

public interface IThemeService
{
    string GetActiveThemeId();
    void ApplyTheme(string themeId);
    void ApplyCustomTheme(string bg, string accent, string card, string text);
    (string bg, string accent, string card, string text) GetCustomColors();
    void ApplyOnStartup();

    // Per-month themes
    Task<List<MonthlyThemeEntity>> GetMonthlyThemesAsync(int year);
    Task SetMonthlyThemeAsync(int year, int month, string? themeId);
    Task<string?> GetMonthlyThemeIdAsync(int year, int month);

    // Contextual theme preview (does NOT save to Preferences)
    void ApplyMonthlyPreview(string themeId);
    void RevertToGlobal();
    event Action? ThemeChanged;
}

public class ThemeService : IThemeService
{
    public event Action? ThemeChanged;
    private readonly DatabaseService _db;

    private const string KeyThemeId = "active_theme";
    private const string KeyCustomBg = "custom_bg";
    private const string KeyCustomAccent = "custom_accent";
    private const string KeyCustomCard = "custom_card";
    private const string KeyCustomText = "custom_text";

    public ThemeService(DatabaseService db)
    {
        _db = db;
    }

    public string GetActiveThemeId() =>
        Preferences.Get(KeyThemeId, "default");

    public void ApplyTheme(string themeId)
    {
        Preferences.Set(KeyThemeId, themeId);
        var theme = AppThemes.GetById(themeId);
        ApplyColorsToResources(
            theme.Background,
            theme.Accent,
            theme.Card,
            theme.TextPrimary,
            theme.TextSecondary,
            theme.Border);

        ApplyTabBarColors(theme.Accent, theme.Card, theme.TextSecondary);
        ThemeChanged?.Invoke();
        ReloadShell();
    }

    public void ApplyCustomTheme(string bg, string accent, string card, string text)
    {
        if (!IsValidHex(bg) || !IsValidHex(accent) || !IsValidHex(card) || !IsValidHex(text))
        {
            Shell.Current.DisplayAlertAsync(
                "Invalid color",
                "Please enter valid hex colors (e.g. #ff6b6b)",
                "OK");
            return;
        }

        Preferences.Set(KeyThemeId, "custom");
        Preferences.Set(KeyCustomBg, bg);
        Preferences.Set(KeyCustomAccent, accent);
        Preferences.Set(KeyCustomCard, card);
        Preferences.Set(KeyCustomText, text);

        ApplyColorsToResources(bg, accent, card, text, text, "#E8E8ED");
        ApplyTabBarColors(accent, card, text);
        ThemeChanged?.Invoke();
        ReloadShell();
    }

    public (string bg, string accent, string card, string text) GetCustomColors() =>
    (
        Preferences.Get(KeyCustomBg, "#f5f5f7"),
        Preferences.Get(KeyCustomAccent, "#ff6b6b"),
        Preferences.Get(KeyCustomCard, "#ffffff"),
        Preferences.Get(KeyCustomText, "#1d1d1f")
    );

    public void ApplyOnStartup()
    {
        var id = GetActiveThemeId();
        if (id == "custom")
        {
            var (bg, accent, card, text) = GetCustomColors();
            ApplyColorsToResources(bg, accent, card, text, text, "#E8E8ED");
        }
        else
        {
            var theme = AppThemes.GetById(id);
            ApplyColorsToResources(
                theme.Background, theme.Accent, theme.Card,
                theme.TextPrimary, theme.TextSecondary, theme.Border);
        }
    }

    private async Task ApplyMonthlyOverrideAsync()
    {
        try
        {
            var now = DateTime.Now;
            var monthlyId = await GetMonthlyThemeIdAsync(now.Year, now.Month);
            if (string.IsNullOrEmpty(monthlyId)) return;

            var theme = AppThemes.GetById(monthlyId);
            ApplyColorsToResources(
                theme.Background,
                theme.Accent,
                theme.Card,
                theme.TextPrimary,
                theme.TextSecondary,
                theme.Border);
            ApplyTabBarColors(theme.Accent, theme.Card, theme.TextSecondary);
        }
        catch
        {
            // DB not ready or no monthly theme — global theme stays
        }
    }

    // ── Per-month theme persistence ─────────────────────────────────────────

    public async Task<List<MonthlyThemeEntity>> GetMonthlyThemesAsync(int year)
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<MonthlyThemeEntity>().ToListAsync();
        return all.Where(r => r.Year == year).ToList();
    }

    public async Task SetMonthlyThemeAsync(int year, int month, string? themeId)
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<MonthlyThemeEntity>().ToListAsync();
        var existing = all.FirstOrDefault(r => r.Year == year && r.Month == month);

        if (string.IsNullOrEmpty(themeId) || themeId == "none")
        {
            // Remove assignment → use global theme for this month
            if (existing is not null)
                await conn.DeleteAsync(existing);
            return;
        }

        if (existing is not null)
        {
            existing.ThemeId = themeId;
            await conn.UpdateAsync(existing);
        }
        else
        {
            await conn.InsertAsync(new MonthlyThemeEntity
            {
                Year = year,
                Month = month,
                ThemeId = themeId
            });
        }
    }

    public async Task<string?> GetMonthlyThemeIdAsync(int year, int month)
    {
        var conn = await _db.GetConnectionAsync();
        var all = await conn.Table<MonthlyThemeEntity>().ToListAsync();
        var row = all.FirstOrDefault(r => r.Year == year && r.Month == month);
        return row?.ThemeId;
    }

    /// <summary>
    /// Applies a theme visually WITHOUT saving to Preferences.
    /// Used when Feed/Budget is viewing a month with an assigned theme.
    /// </summary>
    public void ApplyMonthlyPreview(string themeId)
    {
        var theme = AppThemes.GetById(themeId);
        ApplyColorsToResources(
            theme.Background, theme.Accent, theme.Card,
            theme.TextPrimary, theme.TextSecondary, theme.Border);
        ApplyTabBarColors(theme.Accent, theme.Card, theme.TextSecondary);
        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// Restores the global theme (from Preferences) without any monthly override.
    /// </summary>
    public void RevertToGlobal()
    {
        var id = GetActiveThemeId();
        if (id == "custom")
        {
            var (bg, accent, card, text) = GetCustomColors();
            ApplyColorsToResources(bg, accent, card, text, text, "#E8E8ED");
            ApplyTabBarColors(accent, card, text);
        }
        else
        {
            var theme = AppThemes.GetById(id);
            ApplyColorsToResources(
                theme.Background, theme.Accent, theme.Card,
                theme.TextPrimary, theme.TextSecondary, theme.Border);
            ApplyTabBarColors(theme.Accent, theme.Card, theme.TextSecondary);
        }
        ThemeChanged?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyColorsToResources(
        string bg, string accent, string card,
        string textPrimary, string textSecondary, string border)
    {
        if (Application.Current?.Resources is not ResourceDictionary res) return;

        void Set(string key, string hex)
        {
            if (Color.TryParse(hex, out var color))
                res[key] = color;
        }

        Set("PageBackground", bg);
        Set("CardBackground", card);
        Set("CardBorder", border);
        Set("TextPrimary", textPrimary);
        Set("TextSecondary", textSecondary);
        Set("AccentColor", accent);

        if (Color.TryParse(accent, out var accentColor))
            res["AccentColorAlpha"] = accentColor.WithAlpha(0x1F / 255f);

        App.CurrentAccent = accent;
    }

    private static void ApplyTabBarColors(string accent, string card, string subtext)
    {
        if (Application.Current?.MainPage is not Shell shell) return;
        if (!Color.TryParse(accent, out var accentColor)) return;
        if (!Color.TryParse(card, out var cardColor)) return;
        if (!Color.TryParse(subtext, out var subtextColor)) return;

        Shell.SetTabBarBackgroundColor(shell, cardColor);
        Shell.SetTabBarForegroundColor(shell, accentColor);
        Shell.SetTabBarTitleColor(shell, accentColor);
        Shell.SetTabBarUnselectedColor(shell, subtextColor);

        if (shell is AppShell appShell)
            appShell.RefreshAccent();
    }

    private static bool IsValidHex(string hex) =>
        !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out _);

    public static void ReloadShell()
    {
        if (Application.Current is null) return;

        // Set accent BEFORE rebuilding the shell so AppShell reads the new value
        // (App.CurrentAccent is already set by ApplyColorsToResources)
        Application.Current.MainPage = new AppShell();
    }
}

