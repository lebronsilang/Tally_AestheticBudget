using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class FeedPage : ContentPage
{
    private readonly FeedViewModel _viewModel;

    // Track last known column count so we only rebuild when it actually changes
    private int _lastColumnCount = 0;

    // Prevents OnSizeAllocated from re-triggering a rebuild on page reappear
    // when the column count hasn't changed — which caused image cards to enlarge.
    private bool _gridPopulated = false;

    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Rebuild columns whenever the ViewModel finishes loading data.
        // Also resets _gridPopulated so the fresh data always renders.
        _viewModel.ColumnsRebuilt += () =>
        {
            _gridPopulated = false;
            RebuildMasonryGrid();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnPageAppearingAsync();
    }

    // Fires when the page (window) is resized — recalculate columns if count changed.
    // Also fires on page reappear, so we guard against unnecessary rebuilds
    // that cause the image card to enlarge on navigation back.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0) return;

        var newColumnCount = GetColumnCount(width);

        // Skip if column count hasn't changed AND grid is already built.
        // This prevents the reappear-trigger from re-rendering image cards.
        if (newColumnCount == _lastColumnCount && _gridPopulated) return;

        _lastColumnCount = newColumnCount;
        _gridPopulated = false;  // reset so RebuildMasonryGrid can mark it done

        _viewModel.DistributeIntoColumns(columnCount: newColumnCount);
    }

    // ── Column count breakpoints ──────────────────────────────────────────────
    // Tuned for laptop/desktop — adjust thresholds to taste
    private static int GetColumnCount(double pageWidth) => pageWidth switch
    {
        < 600 => 2,
        < 900 => 3,
        < 1200 => 4,
        _ => 5
    };

    // ── Masonry grid builder ──────────────────────────────────────────────────
    // Called every time ColumnsRebuilt fires (after load or resize).
    // Clears MasonryGrid and rebuilds it with the current Columns from the ViewModel.
    private void RebuildMasonryGrid()
    {
        // Must run on UI thread since we're touching views
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var columns = _viewModel.Columns;
            if (columns.Count == 0) return;

            // Clear previous column definitions and children
            MasonryGrid.ColumnDefinitions.Clear();
            MasonryGrid.Children.Clear();

            // Add one star-width column definition per column
            for (int i = 0; i < columns.Count; i++)
                MasonryGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            // Build a VerticalStackLayout for each column and wire its BindableLayout
            for (int i = 0; i < columns.Count; i++)
            {
                var stack = new VerticalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Start
                };

                // Wire the items source for this column
                BindableLayout.SetItemsSource(stack, columns[i]);

                // Wire the item template — each card uses the shared DataTemplate
                BindableLayout.SetItemTemplate(stack, BuildCardTemplate());

                Grid.SetColumn(stack, i);
                MasonryGrid.Children.Add(stack);
            }

            // Mark grid as populated so OnSizeAllocated skips redundant rebuilds
            _gridPopulated = true;
        });
    }

    // ── Card DataTemplate factory ─────────────────────────────────────────────
    // Photo cards: full-bleed image with gradient overlay + white text on top.
    // No-photo cards: plain white card with themed text — unchanged from before.
    private DataTemplate BuildCardTemplate()
    {
        return new DataTemplate(() =>
        {
            // ── Outer border (the card itself) ────────────────────────────────
            var card = new Border
            {
                StrokeThickness = 0.8,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Color.FromArgb("#14000000")),
                    Offset = new Point(0, 2),
                    Radius = 12,
                    Opacity = 1f
                }
            };
            card.SetDynamicResource(Border.BackgroundColorProperty, "CardBackground");
            card.SetDynamicResource(Border.StrokeProperty, "CardBorder");
            card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(18)
            };

            // Tap → OpenDetailCommand
            var tap = new TapGestureRecognizer();
            tap.SetBinding(TapGestureRecognizer.CommandProperty,
                new Binding("OpenDetailCommand",
                    source: new RelativeBindingSource(
                        RelativeBindingSourceMode.FindAncestorBindingContext,
                        typeof(FeedViewModel))));
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
            card.GestureRecognizers.Add(tap);

            // ── Root grid — photo cards use a single-row overlay layout,
            //               no-photo cards use the classic stacked layout ──────
            var root = new Grid();

            var pointer = new PointerGestureRecognizer();
            card.GestureRecognizers.Add(pointer);

            // ── PHOTO CARD BRANCH ─────────────────────────────────────────────
            // Full-bleed photo fills the card. Gradient + text sit on top via Grid layering.

            var photoContainer = new Grid();
            photoContainer.SetBinding(Grid.IsVisibleProperty, "HasPhoto");

            // Full-bleed image — natural ratio (Pinterest style), hard cap at 500 to prevent blowup
            var photo = new Image
            {
                Aspect = Aspect.AspectFit,
                HeightRequest = -1,
                MaximumHeightRequest = 500,
                HorizontalOptions = LayoutOptions.Fill,
            };
            photo.SetBinding(Image.SourceProperty, "PhotoPath");
            photo.Clip = new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry
            {
                Rect = new Rect(0, 0, 1000, 2000),
                CornerRadius = new CornerRadius(18)
            };
            photoContainer.Children.Add(photo);

            // Gradient overlay — dark at bottom, transparent at top
            // Gives enough contrast for white text without hiding the photo
            var gradient = new BoxView
            {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill,
            };
            gradient.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),   // top
                EndPoint = new Point(0, 1),   // bottom
                GradientStops =
                [
                    new GradientStop { Color = Colors.Transparent,           Offset = 0.0f },
                    new GradientStop { Color = Color.FromArgb("#40000000"),   Offset = 0.45f },
                    new GradientStop { Color = Color.FromArgb("#CC000000"),   Offset = 1.0f },
                ]
            };
            // Clip gradient to match card rounding
            gradient.Clip = new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry
            {
                Rect = new Rect(0, 0, 1000, 2000),
                CornerRadius = new CornerRadius(18)
            };
            photoContainer.Children.Add(gradient);

            // Overlay text — pinned to the bottom of the photo
            var overlayText = new VerticalStackLayout
            {
                Spacing = 3,
                VerticalOptions = LayoutOptions.End,
                Padding = new Thickness(14, 0, 14, 14),
            };

            // Category row (label + grocery badge) — white/semi-white on overlay
            var overlayCatRow = new HorizontalStackLayout { Spacing = 6 };

            var overlayCatLabel = new Label
            {
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.5,
                TextTransform = TextTransform.Uppercase,
                TextColor = Color.FromArgb("#CCffffff"),  // 80% white
            };
            overlayCatLabel.SetBinding(Label.TextProperty, "CategoryLabel");
            overlayCatRow.Children.Add(overlayCatLabel);

            var overlayBadge = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#33ffffff"),  // subtle white pill
                Padding = new Thickness(6, 2),
            };
            overlayBadge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(980)
            };
            overlayBadge.SetBinding(Border.IsVisibleProperty, "IsGroceryGroup");
            var overlayBadgeLabel = new Label
            {
                FontSize = 10,
                TextColor = Colors.White,
            };
            overlayBadgeLabel.SetBinding(Label.TextProperty, "GroceryItemCountLabel");
            overlayBadge.Content = overlayBadgeLabel;
            overlayCatRow.Children.Add(overlayBadge);

            overlayText.Children.Add(overlayCatRow);

            // Title — bold white
            var overlayTitle = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.WordWrap,
            };
            overlayTitle.SetBinding(Label.TextProperty, "Title");
            overlayTitle.SetBinding(Label.IsVisibleProperty, "HasTitle");
            overlayText.Children.Add(overlayTitle);

            // Note — softer white
            var overlayNote = new Label
            {
                FontSize = 12,
                TextColor = Color.FromArgb("#CCffffff"),
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2,
            };
            overlayNote.SetBinding(Label.TextProperty, "Note");
            overlayNote.SetBinding(Label.IsVisibleProperty, "HasNote");
            overlayText.Children.Add(overlayNote);

            // Amount — full white, bold
            var overlayAmount = new Label
            {
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
            };
            overlayAmount.SetBinding(Label.TextProperty, "AmountFormatted");
            overlayText.Children.Add(overlayAmount);

            // Date — dimmer white
            var overlayDate = new Label
            {
                FontSize = 11,
                TextColor = Color.FromArgb("#99ffffff"),
                Margin = new Thickness(0, 2, 0, 0),
            };
            overlayDate.SetBinding(Label.TextProperty,
                new Binding("Date", converter: new Converters.DateToDisplayConverter()));
            overlayText.Children.Add(overlayDate);

            photoContainer.Children.Add(overlayText);
            root.Children.Add(photoContainer);

            // ── NO-PHOTO CARD BRANCH ──────────────────────────────────────────
            // Plain themed card — exactly the same as before

            var plainCard = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),  // content
                    new RowDefinition(GridLength.Auto),  // date
                }
            };
            plainCard.SetBinding(Grid.IsVisibleProperty,
                new Binding("HasPhoto", converter: new Converters.InverseBoolConverter()));

            // ── GROCERY CARD — compact, matches web .card-no-img style ──────
            var groceryCard = new VerticalStackLayout
            {
                Spacing = 4,
                Padding = new Thickness(14, 18, 14, 14),
            };
            groceryCard.SetBinding(VisualElement.IsVisibleProperty, "IsGroceryGroup");

            var groceryCatLabel = new Label
            {
                Text = "🛒 Grocery",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.5,
                TextTransform = TextTransform.Uppercase,
            };
            groceryCatLabel.SetDynamicResource(Label.TextColorProperty, "AccentColor");
            groceryCard.Children.Add(groceryCatLabel);

            var groceryCount = new Label { FontSize = 13 };
            groceryCount.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            groceryCount.SetBinding(Label.TextProperty, "GroceryItemCountLabel");
            groceryCard.Children.Add(groceryCount);

            var groceryAmount = new Label
            {
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
            };
            groceryAmount.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            groceryAmount.SetBinding(Label.TextProperty, "AmountFormatted");
            groceryCard.Children.Add(groceryAmount);

            var groceryDate = new Label { FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
            groceryDate.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            groceryDate.SetBinding(Label.TextProperty,
                new Binding("Date", converter: new Converters.DateToDisplayConverter()));
            groceryCard.Children.Add(groceryDate);

            Grid.SetRow(groceryCard, 0);
            plainCard.Children.Add(groceryCard);

            // ── REGULAR EXPENSE CARD (IsGroceryGroup == false) ────────────────
            var regularCard = new VerticalStackLayout { Spacing = 0 };
            regularCard.SetBinding(VisualElement.IsVisibleProperty,
                new Binding("IsGroceryGroup", converter: new Converters.InverseBoolConverter()));

            var content = new VerticalStackLayout
            {
                Spacing = 4,
                Padding = new Thickness(14, 18, 14, 6),
            };

            // Category row
            var catRow = new HorizontalStackLayout { Spacing = 6 };

            var catLabel = new Label
            {
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.5,
                TextTransform = TextTransform.Uppercase,
            };
            catLabel.SetBinding(Label.TextProperty, "CategoryLabel");
            catLabel.SetDynamicResource(Label.TextColorProperty, "AccentColor");
            catRow.Children.Add(catLabel);

            content.Children.Add(catRow);

            var title = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap,
            };
            title.SetBinding(Label.TextProperty, "Title");
            title.SetBinding(Label.IsVisibleProperty, "HasTitle");
            title.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            content.Children.Add(title);

            var note = new Label
            {
                FontSize = 12,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2,
            };
            note.SetBinding(Label.TextProperty, "Note");
            note.SetBinding(Label.IsVisibleProperty, "HasNote");
            note.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            content.Children.Add(note);

            var amount = new Label
            {
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
            };
            amount.SetBinding(Label.TextProperty, "AmountFormatted");
            amount.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            content.Children.Add(amount);

            regularCard.Children.Add(content);

            var date = new Label
            {
                FontSize = 11,
                Margin = new Thickness(14, 0, 14, 12),
            };
            date.SetBinding(Label.TextProperty,
                new Binding("Date", converter: new Converters.DateToDisplayConverter()));
            date.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            regularCard.Children.Add(date);

            Grid.SetRow(regularCard, 0);
            plainCard.Children.Add(regularCard);

            root.Children.Add(plainCard);

            // Wire pointer events for lift animation — scale up + deeper shadow on hover
            pointer.PointerEntered += (s, e) =>
            {
                _ = card.ScaleTo(1.03, 160, Easing.CubicOut);
                card.Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Color.FromArgb("#28000000")),
                    Offset = new Point(0, 8),
                    Radius = 24,
                    Opacity = 1f
                };
            };
            pointer.PointerExited += (s, e) =>
            {
                _ = card.ScaleTo(1.0, 160, Easing.CubicOut);
                card.Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Color.FromArgb("#14000000")),
                    Offset = new Point(0, 2),
                    Radius = 12,
                    Opacity = 1f
                };
            };

            card.Content = root;
            return card;
        });
    }
}