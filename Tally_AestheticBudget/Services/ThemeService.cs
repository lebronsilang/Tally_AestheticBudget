using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

public interface IThemeService
{
    string GetActiveThemeId();
    void ApplyTheme(string themeId);
    void ApplyCustomTheme(string bg, string accent, string card, string text);
    (string bg, string accent, string card, string text) GetCustomColors();
    void ApplyOnStartup();

    // Light / Dark mode (applies to the active preset; custom palette is mode-independent)
    bool IsDarkMode { get; }
    void SetThemeMode(bool dark);

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
    private const string KeyThemeMode = "theme_mode";   // "light" | "dark"
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

    // ── Light / Dark mode ──────────────────────────────────────────────────────

    public bool IsDarkMode =>
        Preferences.Get(KeyThemeMode, "light") == "dark";

    /// <summary>
    /// Persists the chosen mode and re-applies the active global theme (preset or custom)
    /// so the change takes effect immediately. Custom palettes are mode-independent, but we
    /// still re-apply + fire ThemeChanged so the Themes-grid swatches refresh.
    /// </summary>
    public void SetThemeMode(bool dark)
    {
        Preferences.Set(KeyThemeMode, dark ? "dark" : "light");
        RevertToGlobal();   // re-applies active theme in the new mode and fires ThemeChanged
    }

    public void ApplyTheme(string themeId)
    {
        Preferences.Set(KeyThemeId, themeId);
        var p = AppThemes.GetById(themeId).Palette(IsDarkMode);
        ApplyPalette(p);
        ThemeChanged?.Invoke();
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
            ApplyPalette(AppThemes.GetById(id).Palette(IsDarkMode));
        }
    }

    private async Task ApplyMonthlyOverrideAsync()
    {
        try
        {
            var now = DateTime.Now;
            var monthlyId = await GetMonthlyThemeIdAsync(now.Year, now.Month);
            if (string.IsNullOrEmpty(monthlyId)) return;

            ApplyPalette(AppThemes.GetById(monthlyId).Palette(IsDarkMode));
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
    /// Honors the active Light/Dark mode.
    /// </summary>
    public void ApplyMonthlyPreview(string themeId)
    {
        ApplyPalette(AppThemes.GetById(themeId).Palette(IsDarkMode));
        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// Restores the global theme (from Preferences) without any monthly override.
    /// Honors the active Light/Dark mode.
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
            ApplyPalette(AppThemes.GetById(id).Palette(IsDarkMode));
        }
        ThemeChanged?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Single funnel for applying a resolved ThemePalette (resources + tab bar).</summary>
    private static void ApplyPalette(ThemePalette p)
    {
        ApplyColorsToResources(
            p.Background, p.Accent, p.Card,
            p.TextPrimary, p.TextSecondary, p.Border, p.OnAccent);
        ApplyTabBarColors(p.Accent, p.Card, p.TextSecondary);
    }

    private static void ApplyColorsToResources(
        string bg, string accent, string card,
        string textPrimary, string textSecondary, string border,
        string? onAccent = null)
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
        {
            res["AccentColorAlpha"] = accentColor.WithAlpha(0x1F / 255f);

            // Text/icon color for anything sitting ON the accent fill.
            // Presets supply this explicitly (preserves the established look);
            // the custom palette has no designer choice, so we fall back to the
            // higher-contrast of near-black / white per WCAG.
            res["OnAccentColor"] =
                (!string.IsNullOrWhiteSpace(onAccent) && Color.TryParse(onAccent, out var onAccentColor))
                    ? onAccentColor
                    : BestTextColorOn(accentColor);
        }

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

    // ── Accent text-contrast helpers ───────────────────────────────────────────

    private static readonly Color DarkOnAccent = Color.FromArgb("#1d1d1f");

    /// <summary>
    /// Returns near-black or white — whichever has the higher WCAG contrast
    /// ratio against the given accent fill.
    /// </summary>
    private static Color BestTextColorOn(Color accent)
    {
        double bgLum = RelativeLuminance(accent);
        double contrastWithWhite = (1.0 + 0.05) / (bgLum + 0.05);
        double contrastWithDark = (bgLum + 0.05) / (RelativeLuminance(DarkOnAccent) + 0.05);
        return contrastWithDark >= contrastWithWhite ? DarkOnAccent : Colors.White;
    }

    /// <summary>WCAG 2.1 relative luminance of an sRGB color.</summary>
    private static double RelativeLuminance(Color c)
    {
        static double Linearize(double channel) =>
            channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

        return 0.2126 * Linearize(c.Red)
             + 0.7152 * Linearize(c.Green)
             + 0.0722 * Linearize(c.Blue);
    }

    private static bool IsValidHex(string hex) =>
        !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out _);
}