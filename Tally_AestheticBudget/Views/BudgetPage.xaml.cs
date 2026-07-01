using Microsoft.Maui.Graphics;
using Tally_AestheticBudget.Services;
using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class BudgetPage : ContentPage
{
    private readonly BudgetViewModel _viewModel;
    private readonly IThemeService _themeService;

    public BudgetPage(BudgetViewModel viewModel, IThemeService themeService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _themeService = themeService;
        BindingContext = viewModel;

        // When the drawable's data changes, invalidate the GraphicsView on the UI thread.
        _viewModel.DonutDrawable.Invalidated += OnDonutDataChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe before awaiting so we don't miss a ThemeChanged fired during load.
        _themeService.ThemeChanged += OnThemeChanged;

        // Sync colors with whatever theme is currently active.
        ApplyDonutTheme();

        await _viewModel.OnPageAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _themeService.ThemeChanged -= OnThemeChanged;
        _viewModel.DonutDrawable.Invalidated -= OnDonutDataChanged;  // prevent leak
        _viewModel.OnPageDisappearing();
    }

    // ── Responsive layout ─────────────────────────────────────────────────────

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        // Below 700 dp the side-by-side donut column becomes too cramped;
        // switch to the stacked (donut-above-list) layout instead.
        _viewModel.IsNarrowBudgetLayout = width < 700;
    }

    // ── Dynamic donut sizing ──────────────────────────────────────────────────

    /// <summary>
    /// Layout B (wide): sets the donut height equal to the column width so the
    /// chart always fills a perfect square, scaling with any window resize.
    /// </summary>
    private void OnDonutViewSizeChanged(object sender, EventArgs e)
    {
        if (DonutView?.Width > 0)
            DonutView.HeightRequest = DonutView.Width;
    }

    /// <summary>
    /// Layout C (narrow): same square-aspect logic, applied to the full-width
    /// stacked donut.
    /// </summary>
    private void OnDonutViewNarrowSizeChanged(object sender, EventArgs e)
    {
        if (DonutViewNarrow?.Width > 0)
            DonutViewNarrow.HeightRequest = DonutViewNarrow.Width;
    }

    // ── Donut invalidation ────────────────────────────────────────────────────

    private void OnDonutDataChanged()
    {
        // Invalidated is fired from the ViewModel thread (could be a thread-pool thread
        // after an await); always marshal back to the UI thread before calling Invalidate.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DonutView?.Invalidate();
            DonutViewNarrow?.Invalidate();
        });
    }

    private void OnThemeChanged()
    {
        // Theme colors changed — update the drawable and re-render both views.
        ApplyDonutTheme();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DonutView?.Invalidate();
            DonutViewNarrow?.Invalidate();
        });
    }

    /// <summary>
    /// Reads the six live color tokens from the active resource dictionary and
    /// pushes them into DonutDrawable.  Safe to call at any time.
    /// </summary>
    private void ApplyDonutTheme()
    {
        if (Application.Current?.Resources is not ResourceDictionary res) return;

        _viewModel.DonutDrawable.ApplyTheme(
            GetColor(res, "AccentColor"),
            GetColor(res, "AccentColorAlpha"),
            GetColor(res, "TextPrimary"),
            GetColor(res, "TextSecondary"),
            GetColor(res, "CardBackground"),
            GetColor(res, "CardBorder"));
    }

    private static Color GetColor(ResourceDictionary res, string key)
    {
        if (res.TryGetValue(key, out var val) && val is Color c) return c;
        return Colors.Gray;  // safe fallback; means the key was missing or not a Color
    }
}