using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppTheme = Tally_AestheticBudget.Models.AppTheme;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget.ViewModels;

public partial class ThemesViewModel : ObservableObject
{
    private readonly IThemeService _themeService;

    public ThemesViewModel(IThemeService themeService)
    {
        _themeService = themeService;

        var activeId = _themeService.GetActiveThemeId();

        // Build the card list with IsActive set correctly
        ThemeCards = new ObservableCollection<ThemeCardItem>(
            AppThemes.All.Select(t => new ThemeCardItem
            {
                Theme = t,
                IsActive = t.Id == activeId
            })
        );

        // Load saved custom colors
        var (bg, accent, card, text) = _themeService.GetCustomColors();
        _customBg = bg;
        _customAccent = accent;
        _customCard = card;
        _customText = text;
    }

    // ── Theme cards ───────────────────────────────────────────────────────────

    public ObservableCollection<ThemeCardItem> ThemeCards { get; }

    // ── Custom palette ────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _customBg;

    [ObservableProperty]
    private string _customAccent;

    [ObservableProperty]
    private string _customCard;

    [ObservableProperty]
    private string _customText;

    // ── Color picker overlay ──────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isColorPickerVisible;

    [ObservableProperty]
    private string _pickerTitle = string.Empty;

    private string _pickerTarget = string.Empty; // "bg" | "accent" | "card" | "text"

    public static readonly IReadOnlyList<string> PaletteColors =
    [
        // Whites / lights
        "#ffffff", "#f5f5f7", "#fff0f5", "#f2ede8", "#eef4fb", "#edf4ee", "#0f0f14",
        // Pinks / reds
        "#ff6b6b", "#e8739a", "#ff3b30", "#ff2d55", "#ff6b81", "#ff4757", "#c0392b",
        // Purples
        "#a78bfa", "#9b59b6", "#8e44ad", "#6c5ce7", "#a29bfe", "#fd79a8", "#e84393",
        // Blues
        "#3a8fc4", "#0984e3", "#74b9ff", "#0652dd", "#1289a7", "#12cbc4", "#006266",
        // Greens
        "#4a9e5c", "#00b894", "#55efc4", "#badc58", "#6ab04c", "#2ecc71", "#27ae60",
        // Browns / warm
        "#a0714f", "#e17055", "#d35400", "#e67e22", "#f39c12", "#fdcb6e", "#f9ca24",
        // Darks
        "#1d1d1f", "#2c1f14", "#0d2a40", "#1a2e1c", "#2d3436", "#636e72", "#b2bec3",
    ];

    [RelayCommand]
    private void OpenColorPicker(string target)
    {
        _pickerTarget = target;
        PickerTitle = target switch
        {
            "bg"     => "Background Color",
            "accent" => "Accent Color",
            "card"   => "Card Color",
            _        => "Text Color"
        };
        IsColorPickerVisible = true;
    }

    [RelayCommand]
    private void SelectColor(string hex)
    {
        switch (_pickerTarget)
        {
            case "bg":     CustomBg     = hex; break;
            case "accent": CustomAccent = hex; break;
            case "card":   CustomCard   = hex; break;
            case "text":   CustomText   = hex; break;
        }
        IsColorPickerVisible = false;
    }

    [RelayCommand]
    private void DismissColorPicker() => IsColorPickerVisible = false;

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ApplyTheme(string themeId)
    {
        _themeService.ApplyTheme(themeId);

        // Update IsActive on all cards so borders refresh instantly
        foreach (var card in ThemeCards)
            card.IsActive = card.Id == themeId;
    }

    [RelayCommand]
    private void ApplyCustomTheme()
    {
        _themeService.ApplyCustomTheme(CustomBg, CustomAccent, CustomCard, CustomText);

        // Deactivate all preset cards when custom is applied
        foreach (var card in ThemeCards)
            card.IsActive = false;
    }
}