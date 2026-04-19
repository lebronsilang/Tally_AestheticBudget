using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class FeedPage : ContentPage
{
    private readonly FeedViewModel _viewModel;
    private int _lastColumnCount = 0;
    private bool _gridPopulated = false;

    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

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

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0) return;

        var newColumnCount = GetColumnCount(width);

        if (newColumnCount == _lastColumnCount && _gridPopulated) return;

        _lastColumnCount = newColumnCount;
        _gridPopulated = false;

        _viewModel.DistributeIntoColumns(columnCount: newColumnCount);
    }

    private static int GetColumnCount(double pageWidth) => pageWidth switch
    {
        < 600 => 2,
        < 900 => 3,
        < 1200 => 4,
        _ => 5
    };

    private void RebuildMasonryGrid()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var columns = _viewModel.Columns;
            if (columns.Count == 0) return;

            MasonryGrid.ColumnDefinitions.Clear();
            MasonryGrid.Children.Clear();

            for (int i = 0; i < columns.Count; i++)
                MasonryGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            for (int i = 0; i < columns.Count; i++)
            {
                var stack = new VerticalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Start
                };

                BindableLayout.SetItemsSource(stack, columns[i]);
                BindableLayout.SetItemTemplate(stack, BuildCardTemplate());

                Grid.SetColumn(stack, i);
                MasonryGrid.Children.Add(stack);
            }

            _gridPopulated = true;
        });
    }

    private DataTemplate BuildCardTemplate()
    {
        return new DataTemplate(() =>
        {
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

            var tap = new TapGestureRecognizer();
            tap.SetBinding(TapGestureRecognizer.CommandProperty,
                new Binding("OpenDetailCommand",
                    source: new RelativeBindingSource(
                        RelativeBindingSourceMode.FindAncestorBindingContext,
                        typeof(FeedViewModel))));
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
            card.GestureRecognizers.Add(tap);

            var root = new Grid();
            var pointer = new PointerGestureRecognizer();
            card.GestureRecognizers.Add(pointer);

            // ── PHOTO CARD ────────────────────────────────────────────────
            var photoContainer = new Grid();
            photoContainer.SetBinding(Grid.IsVisibleProperty, "HasPhoto");

            var photo = new Image
            {
                Aspect = Aspect.AspectFit,
                MinimumHeightRequest = 180,
                MaximumHeightRequest = 520,
                HorizontalOptions = LayoutOptions.Fill,
            };
            photo.SetBinding(Image.SourceProperty, "PhotoPath");
            photo.Clip = new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry
            {
                Rect = new Rect(0, 0, 1000, 2000),
                CornerRadius = new CornerRadius(18)
            };
            photoContainer.Children.Add(photo);

            var gradient = new BoxView
            {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill,
            };
            gradient.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                [
                    new GradientStop { Color = Colors.Transparent,         Offset = 0.0f },
                    new GradientStop { Color = Color.FromArgb("#40000000"), Offset = 0.45f },
                    new GradientStop { Color = Color.FromArgb("#CC000000"), Offset = 1.0f },
                ]
            };
            gradient.Clip = new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry
            {
                Rect = new Rect(0, 0, 1000, 2000),
                CornerRadius = new CornerRadius(18)
            };
            photoContainer.Children.Add(gradient);

            // Overlay: Category · Date → Title → Amount → Note
            var overlayText = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.End,
                Padding = new Thickness(14, 0, 14, 14),
            };

            // Row 1: Category · Date
            var overlayCatDateRow = new HorizontalStackLayout { Spacing = 5 };
            var overlayCatLabel = new Label
            {
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.5,
                TextTransform = TextTransform.Uppercase,
                TextColor = Color.FromArgb("#CCffffff"),
            };
            overlayCatLabel.SetBinding(Label.TextProperty, "CategoryLabel");

            var overlaySep = new Label
            {
                Text = "·",
                FontSize = 11,
                TextColor = Color.FromArgb("#99ffffff"),
                VerticalOptions = LayoutOptions.Center,
            };

            var overlayDateInline = new Label
            {
                FontSize = 11,
                TextColor = Color.FromArgb("#99ffffff"),
            };
            overlayDateInline.SetBinding(Label.TextProperty,
                new Binding("Date", converter: new Converters.DateToDisplayConverter()));

            overlayCatDateRow.Children.Add(overlayCatLabel);
            overlayCatDateRow.Children.Add(overlaySep);
            overlayCatDateRow.Children.Add(overlayDateInline);
            overlayText.Children.Add(overlayCatDateRow);

            // Grocery item count badge
            var overlayBadge = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#33ffffff"),
                Padding = new Thickness(6, 2),
                Margin = new Thickness(0, 2, 0, 0),
            };
            overlayBadge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(980)
            };
            overlayBadge.SetBinding(Border.IsVisibleProperty, "IsGroceryGroup");
            var overlayBadgeLabel = new Label { FontSize = 10, TextColor = Colors.White };
            overlayBadgeLabel.SetBinding(Label.TextProperty, "GroceryItemCountLabel");
            overlayBadge.Content = overlayBadgeLabel;
            overlayText.Children.Add(overlayBadge);

            // Row 2: Title
            var overlayTitle = new Label
            {
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.WordWrap,
                Margin = new Thickness(0, 4, 0, 0),
            };
            overlayTitle.SetBinding(Label.TextProperty, "Title");
            overlayTitle.SetBinding(Label.IsVisibleProperty, "HasTitle");
            overlayText.Children.Add(overlayTitle);

            // Row 3: Amount
            var overlayAmount = new Label
            {
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
            };
            overlayAmount.SetBinding(Label.TextProperty, "AmountFormatted");
            overlayText.Children.Add(overlayAmount);

            // Row 4: Note
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

            photoContainer.Children.Add(overlayText);
            root.Children.Add(photoContainer);

            // ── NO-PHOTO CARD ─────────────────────────────────────────────
            var plainCard = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                }
            };
            plainCard.SetBinding(Grid.IsVisibleProperty,
                new Binding("HasPhoto", converter: new Converters.InverseBoolConverter()));

            // ── GROCERY VARIANT ───────────────────────────────────────────
            // Category · Date → item count → Amount → (no note)
            var groceryCard = new VerticalStackLayout
            {
                Spacing = 3,
                Padding = new Thickness(14, 16, 14, 14),
            };
            groceryCard.SetBinding(VisualElement.IsVisibleProperty, "IsGroceryGroup");

            // Row 1: Category · Date
            var groceryCatDateRow = new HorizontalStackLayout { Spacing = 5 };
            var groceryCatLabel = new Label
            {
                Text = "🛒 Grocery",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.5,
                TextTransform = TextTransform.Uppercase,
            };
            groceryCatLabel.SetDynamicResource(Label.TextColorProperty, "AccentColor");

            var grocerySep = new Label
            {
                Text = "·",
                FontSize = 11,
                VerticalOptions = LayoutOptions.Center,
            };
            grocerySep.SetDynamicResource(Label.TextColorProperty, "TextSecondary");

            var groceryDateInline = new Label { FontSize = 11 };
            groceryDateInline.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            groceryDateInline.SetBinding(Label.TextProperty,
                new Binding("Date", converter: new Converters.DateToDisplayConverter()));

            groceryCatDateRow.Children.Add(groceryCatLabel);
            groceryCatDateRow.Children.Add(grocerySep);
            groceryCatDateRow.Children.Add(groceryDateInline);
            groceryCard.Children.Add(groceryCatDateRow);

            // Row 2: Item count (acts as title equivalent for grocery)
            var groceryCount = new Label
            {
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 3, 0, 0),
            };
            groceryCount.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            groceryCount.SetBinding(Label.TextProperty, "GroceryItemCountLabel");
            groceryCard.Children.Add(groceryCount);

            // Row 3: Amount
            var groceryAmount = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
            };
            groceryAmount.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            groceryAmount.SetBinding(Label.TextProperty, "AmountFormatted");
            groceryCard.Children.Add(groceryAmount);

            Grid.SetRow(groceryCard, 0);
            plainCard.Children.Add(groceryCard);

            // ── REGULAR EXPENSE VARIANT ───────────────────────────────────
            // Category · Date → Title → Amount → Note
            var regularCard = new VerticalStackLayout { Spacing = 0 };
            regularCard.SetBinding(VisualElement.IsVisibleProperty,
                new Binding("IsGroceryGroup", converter: new Converters.InverseBoolConverter()));

            var content = new VerticalStackLayout
            {
                Spacing = 3,
                Padding = new Thickness(14, 16, 14, 14),
            };

            // Row 1: Category · Date
            var plainCatDateRow = new HorizontalStackLayout { Spacing = 5 };
            var plainCatLabel = new Label
            {
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 0.5,
                TextTransform = TextTransform.Uppercase,
            };
            plainCatLabel.SetBinding(Label.TextProperty, "CategoryLabel");
            plainCatLabel.SetDynamicResource(Label.TextColorProperty, "AccentColor");

            var plainSep = new Label
            {
                Text = "·",
                FontSize = 11,
                VerticalOptions = LayoutOptions.Center,
            };
            plainSep.SetDynamicResource(Label.TextColorProperty, "TextSecondary");

            var plainDateInline = new Label { FontSize = 11 };
            plainDateInline.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            plainDateInline.SetBinding(Label.TextProperty,
                new Binding("Date", converter: new Converters.DateToDisplayConverter()));

            plainCatDateRow.Children.Add(plainCatLabel);
            plainCatDateRow.Children.Add(plainSep);
            plainCatDateRow.Children.Add(plainDateInline);
            content.Children.Add(plainCatDateRow);

            // Row 2: Title
            var title = new Label
            {
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap,
                Margin = new Thickness(0, 3, 0, 0),
            };
            title.SetBinding(Label.TextProperty, "Title");
            title.SetBinding(Label.IsVisibleProperty, "HasTitle");
            title.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            content.Children.Add(title);

            // Row 3: Amount
            var amount = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
            };
            amount.SetBinding(Label.TextProperty, "AmountFormatted");
            amount.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            content.Children.Add(amount);

            // Row 4: Note
            var note = new Label
            {
                FontSize = 11,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2,
            };
            note.SetBinding(Label.TextProperty, "Note");
            note.SetBinding(Label.IsVisibleProperty, "HasNote");
            note.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            content.Children.Add(note);

            regularCard.Children.Add(content);
            Grid.SetRow(regularCard, 0);
            plainCard.Children.Add(regularCard);

            root.Children.Add(plainCard);

            // ── Hover effects ─────────────────────────────────────────────
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