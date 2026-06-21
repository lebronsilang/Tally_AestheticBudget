namespace Tally_AestheticBudget.Models;

/// <summary>
/// One concrete set of colors (a single light OR dark variant of a theme).
/// Carries its own preview-swatch gradient so the Themes grid can show the
/// correct swatch for whichever mode is active.
/// </summary>
public sealed class ThemePalette
{
    public string PreviewFrom { get; init; } = string.Empty;  // gradient start color for preview swatch
    public string PreviewTo { get; init; } = string.Empty;  // gradient end color for preview swatch

    public string Background { get; init; } = string.Empty;
    public string Card { get; init; } = string.Empty;
    public string Accent { get; init; } = string.Empty;
    public string TextPrimary { get; init; } = string.Empty;
    public string TextSecondary { get; init; } = string.Empty;
    public string Border { get; init; } = string.Empty;

    /// <summary>Text/icon color placed ON the accent fill (buttons, selected chips).
    /// Defaults to white; override when the accent is too pale for white.</summary>
    public string OnAccent { get; init; } = "#FFFFFF";
}

/// <summary>
/// A named theme carrying both a Light and a Dark palette. The active mode
/// (persisted by ThemeService) decides which palette ApplyColorsToResources uses.
/// </summary>
public class AppTheme
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public ThemePalette Light { get; set; } = new();
    public ThemePalette Dark { get; set; } = new();

    /// <summary>Returns the palette for the requested mode.</summary>
    public ThemePalette Palette(bool dark) => dark ? Dark : Light;
}

/// <summary>
/// All 6 preset themes. Light variants preserve the original values exactly;
/// Dark variants are true near-black designs that keep each theme's accent hue.
/// Midnight carries a full light+dark pair like the rest.
/// </summary>
public static class AppThemes
{
    public static readonly IReadOnlyList<AppTheme> All =
    [
        new AppTheme
        {
            Id = "default",
            DisplayName = "Clean Minimal",
            Light = new ThemePalette
            {
                PreviewFrom = "#f5f5f7", PreviewTo = "#e8e8ed",
                Background = "#f5f5f7", Card = "#ffffff", Accent = "#ff6b6b",
                TextPrimary = "#1d1d1f", TextSecondary = "#6e6e73", Border = "#E8E8ED"
            },
            Dark = new ThemePalette
            {
                PreviewFrom = "#131316", PreviewTo = "#ff6b6b",
                Background = "#131316", Card = "#1c1c20", Accent = "#ff6b6b",
                TextPrimary = "#ececef", TextSecondary = "#9a9aa0", Border = "#2a2a30"
            }
        },
        new AppTheme
        {
            Id = "sakura",
            DisplayName = "Sakura Bloom",
            Light = new ThemePalette
            {
                PreviewFrom = "#fff0f5", PreviewTo = "#e8739a",
                Background = "#fff0f5", Card = "#fff8fb", Accent = "#e8739a",
                TextPrimary = "#4a2030", TextSecondary = "#9e6070", Border = "#f4c0d1"
            },
            Dark = new ThemePalette
            {
                PreviewFrom = "#1a1216", PreviewTo = "#e8739a",
                Background = "#1a1216", Card = "#241a1f", Accent = "#e8739a",
                TextPrimary = "#f0e0e6", TextSecondary = "#b89aa4", Border = "#3a2a32"
            }
        },
        new AppTheme
        {
            Id = "mocha",
            DisplayName = "Mocha Latte",
            Light = new ThemePalette
            {
                PreviewFrom = "#f2ede8", PreviewTo = "#a0714f",
                Background = "#f2ede8", Card = "#faf7f4", Accent = "#a0714f",
                TextPrimary = "#2c1f14", TextSecondary = "#7a6050", Border = "#ddd0c4"
            },
            Dark = new ThemePalette
            {
                PreviewFrom = "#17130f", PreviewTo = "#a0714f",
                Background = "#17130f", Card = "#221b15", Accent = "#a0714f",
                TextPrimary = "#ece2d8", TextSecondary = "#b09a86", Border = "#34281e"
            }
        },
        new AppTheme
        {
            Id = "ocean",
            DisplayName = "Ocean Mist",
            Light = new ThemePalette
            {
                PreviewFrom = "#eef4fb", PreviewTo = "#3a8fc4",
                Background = "#eef4fb", Card = "#f6faff", Accent = "#3a8fc4",
                TextPrimary = "#0d2a40", TextSecondary = "#4a7090", Border = "#b5d4f4"
            },
            Dark = new ThemePalette
            {
                PreviewFrom = "#0d1218", PreviewTo = "#3a8fc4",
                Background = "#0d1218", Card = "#161e26", Accent = "#3a8fc4",
                TextPrimary = "#dce8f0", TextSecondary = "#8aa4b8", Border = "#243440"
            }
        },
        new AppTheme
        {
            Id = "forest",
            DisplayName = "Forest Dew",
            Light = new ThemePalette
            {
                PreviewFrom = "#edf4ee", PreviewTo = "#4a9e5c",
                Background = "#edf4ee", Card = "#f6faf6", Accent = "#4a9e5c",
                TextPrimary = "#1a2e1c", TextSecondary = "#4a6e4c", Border = "#c0dd97"
            },
            Dark = new ThemePalette
            {
                PreviewFrom = "#0f140f", PreviewTo = "#4a9e5c",
                Background = "#0f140f", Card = "#181f18", Accent = "#4a9e5c",
                TextPrimary = "#dce8dc", TextSecondary = "#8aa88c", Border = "#243424"
            }
        },
        new AppTheme
        {
            Id = "midnight",
            DisplayName = "Midnight",
            // New light variant — a soft lilac theme that keeps the purple identity.
            Light = new ThemePalette
            {
                PreviewFrom = "#f3f1fb", PreviewTo = "#7c5cfc",
                Background = "#f3f1fb", Card = "#faf9ff", Accent = "#7c5cfc",
                TextPrimary = "#1f1633", TextSecondary = "#6b6090", Border = "#ddd6f4"
            },
            // The original Midnight palette, preserved as the dark variant.
            Dark = new ThemePalette
            {
                PreviewFrom = "#141419", PreviewTo = "#a78bfa",
                Background = "#141419", Card = "#1e1e28", Accent = "#a78bfa",
                TextPrimary = "#cdc8d8", TextSecondary = "#908aa0", Border = "#2e2e3a",
                OnAccent = "#1d1d1f"   // white fails WCAG AA on #a78bfa
            }
        },
    ];

    public static AppTheme GetById(string id) =>
        All.FirstOrDefault(t => t.Id == id) ?? All[0];
}

/// <summary>
/// Wraps a theme with an IsActive flag for the UI to bind to.
/// </summary>
public partial class ThemeCardItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public AppTheme Theme { get; set; } = null!;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isActive;

    /// <summary>The current Light/Dark mode; drives which palette the preview swatch shows.
    /// ThemesViewModel sets this on every card when the mode toggle flips.</summary>
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(PreviewFrom))]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(PreviewTo))]
    private bool _isDarkMode;

    public string Id => Theme.Id;
    public string DisplayName => Theme.DisplayName;
    public string PreviewFrom => Theme.Palette(IsDarkMode).PreviewFrom;
    public string PreviewTo => Theme.Palette(IsDarkMode).PreviewTo;

    /// <summary>
    /// Forces the IsActive-bound BoolToChipBorder converter to re-run even when IsActive's
    /// value is unchanged. Needed because [ObservableProperty] suppresses notification on
    /// no-op assignment, which would otherwise leave a stale accent on the card border
    /// after a theme switch.
    /// </summary>
    public void RaiseActiveChanged() => OnPropertyChanged(nameof(IsActive));
}

/// Wraps one month row for the per-month theme UI.
public partial class MonthlyThemeItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(HasTheme))]
    private string _assignedThemeId = "none";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _assignedThemeName = "Default (global)";

    // Gradient swatch colors for the grid card preview
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _previewFrom = "Transparent";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _previewTo = "Transparent";

    public bool HasTheme => AssignedThemeId != "none";
}