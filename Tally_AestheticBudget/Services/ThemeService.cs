using Tally_AestheticBudget.Models;

namespace Tally_AestheticBudget.Services;

public interface IThemeService
{
    string GetActiveThemeId();
    void ApplyTheme(string themeId);
    void ApplyCustomTheme(string bg, string accent, string card, string text);
    (string bg, string accent, string card, string text) GetCustomColors();
    void ApplyOnStartup();
}

public class ThemeService : IThemeService
{
    private const string KeyThemeId = "active_theme";
    private const string KeyCustomBg = "custom_bg";
    private const string KeyCustomAccent = "custom_accent";
    private const string KeyCustomCard = "custom_card";
    private const string KeyCustomText = "custom_text";

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
                theme.Background,
                theme.Accent,
                theme.Card,
                theme.TextPrimary,
                theme.TextSecondary,
                theme.Border);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Writes theme colors into the app's resource dictionary.
    // Keys must match what's defined in App.xaml.
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

        // Accent at ~12% opacity — used for tinted pill/progress-track backgrounds
        if (Color.TryParse(accent, out var accentColor))
            res["AccentColorAlpha"] = accentColor.WithAlpha(0x1F / 255f);

        App.CurrentAccent = accent;
    }

    // After reloadShell creates a fresh AppShell, push the accent color into the shell's tab bar so it matches the active theme immediately.
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
    }

    private static bool IsValidHex(string hex) =>
        !string.IsNullOrWhiteSpace(hex) && Color.TryParse(hex, out _);
}