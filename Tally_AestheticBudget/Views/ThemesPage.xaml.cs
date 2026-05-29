using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class ThemesPage : ContentPage
{
    public ThemesPage(ThemesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ViewModels.ThemesViewModel vm)
            await vm.LoadMonthlyThemesCommand.ExecuteAsync(null);
    }

    private async void OnThemeCardPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr &&
            pgr.Parent is Border border)
            await border.ScaleTo(1.03, 120, Easing.CubicOut);
    }

    private async void OnThemeCardPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr &&
            pgr.Parent is Border border)
            await border.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    private async void OnSwatchPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr &&
            pgr.Parent is Border border)
            await border.ScaleTo(1.08, 120, Easing.CubicOut);
    }

    private async void OnSwatchPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr &&
            pgr.Parent is Border border)
            await border.ScaleTo(1.0, 120, Easing.CubicOut);
    }

    // Swallow taps inside picker card so they don't bubble to overlay and close it
    private void OnPickerCardTapped(object sender, TappedEventArgs e) { }
}