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

        // build month picker options (all presets + "none")
        var pickerList = new List<ThemePickerOption>
        {
            new() { ThemeId = "none", DisplayName = "Default (global)" }
        };
        pickerList.AddRange(AppThemes.All.Select(t => new ThemePickerOption
        {
            ThemeId = t.Id,
            DisplayName = t.DisplayName
        }));
        ThemePickerOptions = new ObservableCollection<ThemePickerOption>(pickerList);

        _monthlyYear = DateTime.Now.Year;
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

    // ═══════════════════════════════════════════════════════════════════════════
    // ── Per-Month Themes ──────────────────────────────────────────────────────
    // ═══════════════════════════════════════════════════════════════════════════

    public ObservableCollection<MonthlyThemeItem> MonthlyItems { get; } = [];
    public ObservableCollection<ThemePickerOption> ThemePickerOptions { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyYearLabel))]
    private int _monthlyYear;

    public string MonthlyYearLabel => MonthlyYear.ToString();

    // The month item being edited
    [ObservableProperty]
    private MonthlyThemeItem? _editingMonth;

    [ObservableProperty]
    private bool _isMonthPickerVisible;

    [RelayCommand]
    private async Task LoadMonthlyThemesAsync()
    {
        var rows = await _themeService.GetMonthlyThemesAsync(MonthlyYear);
        MonthlyItems.Clear();

        for (int m = 1; m <= 12; m++)
        {
            var row = rows.FirstOrDefault(r => r.Month == m);
            var themeId = row?.ThemeId ?? "none";
            var themeName = themeId == "none"
                ? "Default (global)"
                : AppThemes.All.FirstOrDefault(t => t.Id == themeId)?.DisplayName ?? themeId;

            MonthlyItems.Add(new MonthlyThemeItem
            {
                Year = MonthlyYear,
                Month = m,
                MonthLabel = new DateTime(MonthlyYear, m, 1).ToString("MMMM"),
                AssignedThemeId = themeId,
                AssignedThemeName = themeName
            });
        }
    }

    [RelayCommand]
    private async Task PreviousMonthlyYear()
    {
        MonthlyYear--;
        await LoadMonthlyThemesAsync();
    }

    [RelayCommand]
    private async Task NextMonthlyYear()
    {
        MonthlyYear++;
        await LoadMonthlyThemesAsync();
    }

    [RelayCommand]
    private void OpenMonthThemePicker(MonthlyThemeItem item)
    {
        EditingMonth = item;

        // Highlight current selection
        foreach (var opt in ThemePickerOptions)
            opt.IsSelected = opt.ThemeId == item.AssignedThemeId;

        IsMonthPickerVisible = true;
    }

    [RelayCommand]
    private void DismissMonthPicker() => IsMonthPickerVisible = false;

    [RelayCommand]
    private async Task SelectMonthThemeAsync(ThemePickerOption option)
    {
        if (EditingMonth is null) return;

        await _themeService.SetMonthlyThemeAsync(
            EditingMonth.Year, EditingMonth.Month,
            option.ThemeId == "none" ? null : option.ThemeId);

        EditingMonth.AssignedThemeId = option.ThemeId;
        EditingMonth.AssignedThemeName = option.DisplayName;

        // If this is the current month, live-apply the theme
        var now = DateTime.Now;
        if (EditingMonth.Year == now.Year && EditingMonth.Month == now.Month)
        {
            if (option.ThemeId == "none")
            {
                // Revert to global
                var globalId = _themeService.GetActiveThemeId();
                _themeService.ApplyTheme(globalId);
            }
            else
            {
                _themeService.ApplyTheme(option.ThemeId);
            }

            // Update preset card active states
            var appliedId = option.ThemeId == "none"
                ? _themeService.GetActiveThemeId()
                : option.ThemeId;
            foreach (var card in ThemeCards)
                card.IsActive = card.Id == appliedId;
        }

        IsMonthPickerVisible = false;
    }
}

// ── Helper for theme picker ───────────────────────────────────────────────────

public partial class ThemePickerOption : ObservableObject
{
    public string ThemeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}