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
        _isDarkMode = _themeService.IsDarkMode;

        ThemeCards = new ObservableCollection<ThemeCardItem>(
            AppThemes.All.Select(t => new ThemeCardItem
            {
                Theme = t,
                IsActive = t.Id == activeId,
                IsDarkMode = _isDarkMode
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

    // ── Light / Dark mode ──────────────────────────────────────────────────────

    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        private set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                OnPropertyChanged(nameof(IsLightSelected));
                OnPropertyChanged(nameof(IsDarkSelected));
            }
        }
    }

    // Segment-highlight bindings (drive BoolToChipBg/Text on the header toggle).
    public bool IsLightSelected => !IsDarkMode;
    public bool IsDarkSelected => IsDarkMode;

    [RelayCommand] private void SetLightMode() => ApplyMode(false);
    [RelayCommand] private void SetDarkMode() => ApplyMode(true);

    private void ApplyMode(bool dark)
    {
        if (IsDarkMode == dark) return;
        IsDarkMode = dark;

        // Repaint every preview swatch to the new variant.
        foreach (var card in ThemeCards) card.IsDarkMode = dark;

        // Persist + re-apply the active theme in the new mode (fires ThemeChanged →
        // RefreshActiveCards, which also re-runs the segment converters).
        _themeService.SetThemeMode(dark);
    }

    // ── Theme-change subscription (lifecycle-scoped, not constructor) ──────────
    // ThemesViewModel is registered AddTransient, so subscribing in the constructor
    // would leak an instance per navigation. Instead we subscribe on appear and
    // unsubscribe on disappear, matching every other page's pattern.

    private bool _themeSubscribed;

    public void OnPageAppearing()
    {
        if (!_themeSubscribed)
        {
            _themeSubscribed = true;
            _themeService.ThemeChanged += RefreshActiveCards;
        }
        // Returning from Feed/Budget can leave a stale accent on the card borders if a
        // monthly-preview was reverted while we were away — refresh on every appearance.
        RefreshActiveCards();
    }

    public void OnPageDisappearing()
    {
        _themeService.ThemeChanged -= RefreshActiveCards;
        _themeSubscribed = false;
    }

    // ── Active-card refresh ───────────────────────────────────────────────────

    /// <summary>
    /// Re-evaluates which card is active and FORCES a PropertyChanged on IsActive for
    /// every card — not only the ones whose value flipped. The CommunityToolkit
    /// [ObservableProperty] setter suppresses notification when the value is unchanged,
    /// which would leave the inactive cards' BoolToChipBorder converter holding a stale
    /// App.CurrentAccent. RaiseActiveChanged re-runs the converter unconditionally.
    /// </summary>
    private void RefreshActiveCards()
    {
        var activeId = _themeService.GetActiveThemeId();
        foreach (var card in ThemeCards)
        {
            card.IsActive = card.Id == activeId;  // updates value (notifies only if changed)
            card.RaiseActiveChanged();            // force border converter to re-run regardless
        }

        // The Light/Dark header segment uses BoolToChipBg/Text (accent-dependent) bound to
        // IsLightSelected/IsDarkSelected — force those converters to re-run on theme change.
        OnPropertyChanged(nameof(IsLightSelected));
        OnPropertyChanged(nameof(IsDarkSelected));
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
    private async Task ApplyThemeAsync(string themeId)
    {
        _themeService.ApplyTheme(themeId);

        // Sync only the current month to match the new preset
        await _themeService.SetMonthlyThemeAsync(MonthlyYear, DateTime.Now.Month, themeId);

        await LoadMonthlyThemesAsync();
        await Shell.Current.GoToAsync("//ThemesPage");
        foreach (var card in ThemeCards)
            card.IsActive = card.Id == themeId;
    }

    [RelayCommand]
    private async Task ApplyCustomTheme()
    {
        _themeService.ApplyCustomTheme(CustomBg, CustomAccent, CustomCard, CustomText);
        await Shell.Current.GoToAsync("//ThemesPage");
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

            string themeName;
            string previewFrom = "Transparent";
            string previewTo = "Transparent";

            if (themeId == "none")
            {
                themeName = "Default";
            }
            else
            {
                var theme = AppThemes.All.FirstOrDefault(t => t.Id == themeId);
                themeName = theme?.DisplayName ?? themeId;
                var pal = theme?.Palette(IsDarkMode);
                previewFrom = pal?.PreviewFrom ?? "Transparent";
                previewTo = pal?.PreviewTo ?? "Transparent";
            }

            MonthlyItems.Add(new MonthlyThemeItem
            {
                Year = MonthlyYear,
                Month = m,
                MonthLabel = new DateTime(MonthlyYear, m, 1).ToString("MMM"),
                AssignedThemeId = themeId,
                AssignedThemeName = themeName,
                PreviewFrom = previewFrom,
                PreviewTo = previewTo
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

        // Update the UI card
        EditingMonth.AssignedThemeId = option.ThemeId;
        if (option.ThemeId == "none")
        {
            EditingMonth.AssignedThemeName = "Default";
            EditingMonth.PreviewFrom = "Transparent";
            EditingMonth.PreviewTo = "Transparent";
        }
        else
        {
            var theme = AppThemes.All.FirstOrDefault(t => t.Id == option.ThemeId);
            EditingMonth.AssignedThemeName = theme?.DisplayName ?? option.ThemeId;
            var pal = theme?.Palette(IsDarkMode);
            EditingMonth.PreviewFrom = pal?.PreviewFrom ?? "Transparent";
            EditingMonth.PreviewTo = pal?.PreviewTo ?? "Transparent";
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