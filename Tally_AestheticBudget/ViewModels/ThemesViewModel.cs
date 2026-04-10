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