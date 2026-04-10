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
    // Preferences keys
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
        ApplyColorsToApp(
            theme.Background,
            theme.Accent,
            theme.Card,
            theme.TextPrimary,
            theme.TextSecondary,
            theme.Border);
    }

    public void ApplyCustomTheme(string bg, string accent, string card, string text)
    {
        // Save custom colors to Preferences
        Preferences.Set(KeyThemeId, "custom");
        Preferences.Set(KeyCustomBg, bg);
        Preferences.Set(KeyCustomAccent, accent);
        Preferences.Set(KeyCustomCard, card);
        Preferences.Set(KeyCustomText, text);

        ApplyColorsToApp(bg, accent, card, text, text, "#E8E8ED");
    }

    public (string bg, string accent, string card, string text) GetCustomColors() =>
    (
        Preferences.Get(KeyCustomBg, "#f5f5f7"),
        Preferences.Get(KeyCustomAccent, "#ff6b6b"),
        Preferences.Get(KeyCustomCard, "#ffffff"),
        Preferences.Get(KeyCustomText, "#1d1d1f")
    );

    // Applies colors to Application.Current.Resources so every page updates instantly
    private static void ApplyColorsToApp(
        string bg, string accent, string card,
        string textPrimary, string textSecondary, string border)
    {
        if (Application.Current?.Resources is not ResourceDictionary resources) return;

        // These keys must match what your XAML pages reference.
        // We set them as Color objects so StaticResource bindings pick them up.
        void Set(string key, string hex)
        {
            try
            {
                if (Color.TryParse(hex, out var color))
                    resources[key] = color;
            }
            catch { /* ignore if key doesn't exist yet */ }
        }

        Set("PageBackground", bg);
        Set("CardBackground", card);
        Set("CardBorder", border);
        Set("TextPrimary", textPrimary);
        Set("TextSecondary", textSecondary);
        Set("AccentColor", accent);

        // Also update the hardcoded hex colors used throughout the pages
        // We expose the accent as a static so XAML can reference it
        App.CurrentAccent = accent;
    }

    // Apply saved theme at startup — called from App.xaml.cs
    public void ApplyOnStartup()
    {
        var id = GetActiveThemeId();
        if (id == "custom")
        {
            var (bg, accent, card, text) = GetCustomColors();
            ApplyCustomTheme(bg, accent, card, text);
        }
        else
        {
            ApplyTheme(id);
        }
    }
}