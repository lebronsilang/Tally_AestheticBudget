using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class ThemesPage : ContentPage
{
    private string _editingField = "";

    public ThemesPage(ThemesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // ── Swatch taps ───────────────────────────────────────────────────────────

    private void OnBgSwatchTapped(object sender, TappedEventArgs e)
        => OpenPicker("bg", "Background Color");

    private void OnAccentSwatchTapped(object sender, TappedEventArgs e)
        => OpenPicker("accent", "Accent Color");

    private void OnCardSwatchTapped(object sender, TappedEventArgs e)
        => OpenPicker("card", "Card Color");

    private void OnTextSwatchTapped(object sender, TappedEventArgs e)
        => OpenPicker("text", "Text Color");

    // ── Hover effects ─────────────────────────────────────────────────────────

    private async void OnSwatchPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer { Parent: Border border })
            await border.ScaleTo(1.08, 120, Easing.CubicOut);
    }

    private async void OnSwatchPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer { Parent: Border border })
            await border.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    private async void OnThemeCardPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer { Parent: Border border })
            await border.ScaleTo(1.03, 120, Easing.CubicOut);
    }

    private async void OnThemeCardPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer { Parent: Border border })
            await border.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    // ── Color picker ──────────────────────────────────────────────────────────

    private void OpenPicker(string field, string title)
    {
        var vm = (ThemesViewModel)BindingContext;
        _editingField = field;

        PickerTitle.Text = title;
        PickerEntry.Text = field switch
        {
            "bg" => vm.CustomBg,
            "accent" => vm.CustomAccent,
            "card" => vm.CustomCard,
            "text" => vm.CustomText,
            _ => "#ffffff"
        };

        UpdatePickerPreview(PickerEntry.Text);
        PickerOverlay.IsVisible = true;
        PickerEntry.Focus();
    }

    private void OnPickerEntryChanged(object sender, TextChangedEventArgs e)
        => UpdatePickerPreview(e.NewTextValue);

    private void UpdatePickerPreview(string hex)
    {
        try
        {
            if (hex?.Length >= 4)
                PickerPreview.BackgroundColor = Color.FromArgb(hex);
        }
        catch { /* ignore invalid hex while typing */ }
    }

    private void OnPresetChipTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border chip)
        {
            var c = chip.BackgroundColor;
            var hex = $"#{(int)(c.Red * 255):X2}{(int)(c.Green * 255):X2}{(int)(c.Blue * 255):X2}";
            PickerEntry.Text = hex;
            UpdatePickerPreview(hex);
        }
    }

    private void OnPickerConfirm(object sender, TappedEventArgs e)
    {
        var vm = (ThemesViewModel)BindingContext;
        var hex = PickerEntry.Text?.Trim();
        if (string.IsNullOrEmpty(hex)) { ClosePicker(); return; }
        if (!hex.StartsWith('#')) hex = "#" + hex;

        switch (_editingField)
        {
            case "bg": vm.CustomBg = hex; break;
            case "accent": vm.CustomAccent = hex; break;
            case "card": vm.CustomCard = hex; break;
            case "text": vm.CustomText = hex; break;
        }

        ClosePicker();
    }

    private void OnPickerCancel(object sender, TappedEventArgs e)
        => ClosePicker();

    private void OnPickerOverlayTapped(object sender, TappedEventArgs e)
        => ClosePicker();

    private void OnPickerCardTapped(object sender, TappedEventArgs e) { }

    private void ClosePicker()
    {
        PickerOverlay.IsVisible = false;
        _editingField = "";
    }
}