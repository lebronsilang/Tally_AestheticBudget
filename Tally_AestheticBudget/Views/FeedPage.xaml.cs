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

    // ── Grocery preview row (index 0 or 1) ───────────────────────────────────
    // Shows a single item name + price — Apple HIG style
    private static Grid BuildGroceryPreviewRow(int index)
    {
        var row = new Grid
        {
            Padding = new Thickness(0, 5, 0, 5),
            Opacity = index == 0 ? 1.0 : 0.45,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            }
        };

        var name = new Label
        {
            FontSize = 14,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            VerticalOptions = LayoutOptions.Center,
        };
        name.SetDynamicResource(Label.TextColorProperty, "TextPrimary");

        var price = new Label
        {
            FontSize = 14,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
        };
        price.SetDynamicResource(Label.TextColorProperty, "TextSecondary");

        name.SetBinding(Label.TextProperty, new Binding($"GroceryItems[{index}].Name"));
        price.SetBinding(Label.TextProperty, new Binding($"GroceryItems[{index}].PriceFormatted"));
        row.SetBinding(VisualElement.IsVisibleProperty,
            new Binding("GroceryItems.Count",
                converter: new Converters.IndexVisibilityConverter(),
                converterParameter: index));

        Grid.SetColumn(name, 0);
        Grid.SetColumn(price, 1);
        row.Children.Add(name);
        row.Children.Add(price);

        return row;
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

            // Vignette overlay — fades in on hover like Pinterest
            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += (s, e) => { }; // wired after root is built
            pointer.PointerExited += (s, e) => { };  // wired after root is built
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

            // ── GROCERY CARD — Apple HIG style ───────────────────────────────
            var groceryCard = new VerticalStackLayout { Spacing = 0 };
            groceryCard.SetBinding(VisualElement.IsVisibleProperty, "IsGroceryGroup");

            // Top section: label + amount
            var groceryTop = new Grid
            {
                Padding = new Thickness(16, 16, 16, 10),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                }
            };

            var groceryLeft = new VerticalStackLayout { Spacing = 3 };

            var groceryCatLabel = new Label
            {
                Text = "Grocery",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
            };
            groceryCatLabel.SetDynamicResource(Label.TextColorProperty, "TextSecondary");

            var groceryMeta = new HorizontalStackLayout { Spacing = 5 };
            var groceryCount = new Label { FontSize = 12 };
            groceryCount.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            groceryCount.SetBinding(Label.TextProperty, "GroceryItemCountLabel");
            var groceryMetaSep = new Label { Text = "·", FontSize = 12 };
            groceryMetaSep.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            var groceryMetaDate = new Label { FontSize = 12 };
            groceryMetaDate.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            groceryMetaDate.SetBinding(Label.TextProperty,
                new Binding("Date", converter: new Converters.DateToDisplayConverter()));
            groceryMeta.Children.Add(groceryCount);
            groceryMeta.Children.Add(groceryMetaSep);
            groceryMeta.Children.Add(groceryMetaDate);

            groceryLeft.Children.Add(groceryCatLabel);
            groceryLeft.Children.Add(groceryMeta);

            // Large amount — the hero
            var groceryAmount = new Label
            {
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End,
            };
            groceryAmount.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            groceryAmount.SetBinding(Label.TextProperty, "AmountFormatted");

            Grid.SetColumn(groceryLeft, 0);
            Grid.SetColumn(groceryAmount, 1);
            groceryTop.Children.Add(groceryLeft);
            groceryTop.Children.Add(groceryAmount);
            groceryCard.Children.Add(groceryTop);

            // Hairline divider
            var divider1 = new BoxView { HeightRequest = 0.5, Margin = new Thickness(16, 0) };
            divider1.SetDynamicResource(BoxView.ColorProperty, "CardBorder");
            groceryCard.Children.Add(divider1);

            // Item rows
            var itemsStack = new VerticalStackLayout
            {
                Spacing = 0,
                Padding = new Thickness(16, 8, 16, 4),
            };
            itemsStack.Children.Add(BuildGroceryPreviewRow(0));
            itemsStack.Children.Add(BuildGroceryPreviewRow(1));
            groceryCard.Children.Add(itemsStack);

            // Hairline divider
            var divider2 = new BoxView { HeightRequest = 0.5, Margin = new Thickness(16, 0) };
            divider2.SetDynamicResource(BoxView.ColorProperty, "CardBorder");
            groceryCard.Children.Add(divider2);

            // "View all" row — just a quiet chevron
            var viewAllRow = new Grid
            {
                Padding = new Thickness(16, 10, 16, 14),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                }
            };
            var viewAllLabel = new Label
            {
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center,
            };
            viewAllLabel.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            viewAllLabel.SetBinding(Label.TextProperty,
                new Binding("GroceryItemCountLabel", stringFormat: "See all {0}"));
            var viewAllChevron = new Label
            {
                Text = "›",
                FontSize = 18,
                VerticalOptions = LayoutOptions.Center,
            };
            viewAllChevron.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            Grid.SetColumn(viewAllLabel, 0);
            Grid.SetColumn(viewAllChevron, 1);
            viewAllRow.Children.Add(viewAllLabel);
            viewAllRow.Children.Add(viewAllChevron);
            groceryCard.Children.Add(viewAllRow);

            Grid.SetRow(groceryCard, 0);
            Grid.SetColumnSpan(groceryCard, 1);
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

            // Vignette overlay sits on top of everything, starts invisible
            var vignetteOverlay = new Border
            {
                Opacity = 0,
                InputTransparent = true,
                StrokeThickness = 0,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops =
                    [
                        new GradientStop { Color = Color.FromArgb("#80000000"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#22000000"), Offset = 0.4f },
                        new GradientStop { Color = Colors.Transparent,          Offset = 1.0f },
                    ]
                },
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(18)
                }
            };

            // Wire pointer events to fade this overlay
            pointer.PointerEntered += (s, e) => vignetteOverlay.FadeTo(0.15, 180, Easing.CubicOut);
            pointer.PointerExited += (s, e) => vignetteOverlay.FadeTo(0, 180, Easing.CubicOut);

            root.Children.Add(vignetteOverlay);

            card.Content = root;
            return card;
        });
    }
}