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

        ThemeCards = new ObservableCollection<ThemeCardItem>(
            AppThemes.All.Select(t => new ThemeCardItem
            {
                Theme = t,
                IsActive = t.Id == activeId
            })
        );

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

    [ObservableProperty]
    private string _pickerHexInput = string.Empty;

    private string _pickerTarget = string.Empty;

    [RelayCommand]
    private void OpenColorPicker(string target)
    {
        _pickerTarget = target;
        PickerTitle = target switch
        {
            "bg" => "Background Color",
            "accent" => "Accent Color",
            "card" => "Card Color",
            _ => "Text Color"
        };
        PickerHexInput = target switch
        {
            "bg" => CustomBg,
            "accent" => CustomAccent,
            "card" => CustomCard,
            _ => CustomText
        };
        IsColorPickerVisible = true;
    }

    [RelayCommand]
    private void SelectColor(string hex)
    {
        switch (_pickerTarget)
        {
            case "bg": CustomBg = hex; break;
            case "accent": CustomAccent = hex; break;
            case "card": CustomCard = hex; break;
            case "text": CustomText = hex; break;
        }
        PickerHexInput = hex;
        IsColorPickerVisible = false;
    }

    [RelayCommand]
    private void ApplyHexInput()
    {
        var hex = PickerHexInput?.Trim();
        if (string.IsNullOrEmpty(hex)) return;
        if (!hex.StartsWith('#')) hex = "#" + hex;
        if (hex.Length != 7) return;
        SelectColor(hex);
    }

    [RelayCommand]
    private void DismissColorPicker() => IsColorPickerVisible = false;

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ApplyTheme(string themeId)
    {
        _themeService.ApplyTheme(themeId);
        foreach (var card in ThemeCards)
            card.IsActive = card.Id == themeId;
    }

    [RelayCommand]
    private void ApplyCustomTheme()
    {
        _themeService.ApplyCustomTheme(CustomBg, CustomAccent, CustomCard, CustomText);
        foreach (var card in ThemeCards)
            card.IsActive = false;
    }
}