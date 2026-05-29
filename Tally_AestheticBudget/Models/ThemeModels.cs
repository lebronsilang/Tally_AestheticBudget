namespace Tally_AestheticBudget.Models;

public class AppTheme
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PreviewFrom { get; set; } = string.Empty;  // gradient start color for preview swatch
    public string PreviewTo { get; set; } = string.Empty;  // gradient end color for preview swatch

    // The actual color values applied to the app
    public string Background { get; set; } = string.Empty;
    public string Card { get; set; } = string.Empty;
    public string Accent { get; set; } = string.Empty;
    public string TextPrimary { get; set; } = string.Empty;
    public string TextSecondary { get; set; } = string.Empty;
    public string Border { get; set; } = string.Empty;
}

/// <summary>
/// All 6 preset themes — values match your CSS exactly.
/// </summary>
public static class AppThemes
{
    public static readonly IReadOnlyList<AppTheme> All =
    [
        new AppTheme
        {
            Id           = "default",
            DisplayName  = "Clean Minimal",
            PreviewFrom  = "#f5f5f7",
            PreviewTo    = "#e8e8ed",
            Background   = "#f5f5f7",
            Card         = "#ffffff",
            Accent       = "#ff6b6b",
            TextPrimary  = "#1d1d1f",
            TextSecondary = "#6e6e73",
            Border       = "#E8E8ED"
        },
        new AppTheme
        {
            Id           = "sakura",
            DisplayName  = "Sakura Bloom",
            PreviewFrom  = "#fff0f5",
            PreviewTo    = "#e8739a",
            Background   = "#fff0f5",
            Card         = "#fff8fb",
            Accent       = "#e8739a",
            TextPrimary  = "#4a2030",
            TextSecondary = "#9e6070",
            Border       = "#f4c0d1"
        },
        new AppTheme
        {
            Id           = "mocha",
            DisplayName  = "Mocha Latte",
            PreviewFrom  = "#f2ede8",
            PreviewTo    = "#a0714f",
            Background   = "#f2ede8",
            Card         = "#faf7f4",
            Accent       = "#a0714f",
            TextPrimary  = "#2c1f14",
            TextSecondary = "#7a6050",
            Border       = "#ddd0c4"
        },
        new AppTheme
        {
            Id           = "ocean",
            DisplayName  = "Ocean Mist",
            PreviewFrom  = "#eef4fb",
            PreviewTo    = "#3a8fc4",
            Background   = "#eef4fb",
            Card         = "#f6faff",
            Accent       = "#3a8fc4",
            TextPrimary  = "#0d2a40",
            TextSecondary = "#4a7090",
            Border       = "#b5d4f4"
        },
        new AppTheme
        {
            Id           = "forest",
            DisplayName  = "Forest Dew",
            PreviewFrom  = "#edf4ee",
            PreviewTo    = "#4a9e5c",
            Background   = "#edf4ee",
            Card         = "#f6faf6",
            Accent       = "#4a9e5c",
            TextPrimary  = "#1a2e1c",
            TextSecondary = "#4a6e4c",
            Border       = "#c0dd97"
        },
        new AppTheme
        {
            Id           = "midnight",
            DisplayName  = "Midnight",
            PreviewFrom  = "#141419",
            PreviewTo    = "#a78bfa",
            Background   = "#141419",
            Card         = "#1e1e28",
            Accent       = "#a78bfa",
            TextPrimary  = "#cdc8d8",
            TextSecondary = "#908aa0",
            Border       = "#2e2e3a"
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

    public string Id => Theme.Id;
    public string DisplayName => Theme.DisplayName;
    public string PreviewFrom => Theme.PreviewFrom;
    public string PreviewTo => Theme.PreviewTo;

}

/// Wraps one month row for the per-month theme UI.
public partial class MonthlyThemeItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;  // "January", "February", ...

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _assignedThemeId = "none";

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _assignedThemeName = "Default (global)";
}