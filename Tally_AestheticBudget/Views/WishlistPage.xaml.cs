using Microsoft.Maui.Controls;
using Tally_AestheticBudget.Converters;
using Tally_AestheticBudget.Models;
using Tally_AestheticBudget.ViewModels;

namespace Tally_AestheticBudget.Views;

public partial class WishlistPage : ContentPage
{
    private readonly WishlistViewModel _viewModel;
    private int _lastColumnCount = 0;
    private bool _gridPopulated = false;
    private double _lastWidth = 0;

    public WishlistPage(WishlistViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.FilterChanged += () =>
        {
            MainThread.BeginInvokeOnMainThread(ClearMasonryGrid);
        };

        _viewModel.ColumnsRebuilt += () =>
        {
            MainThread.BeginInvokeOnMainThread(RebuildMasonryGrid);
        };


        _viewModel.DataLoaded += () =>
        {
            _gridPopulated = false;

            var w = MasonryGrid.Width;

            _viewModel.DistributeIntoColumns(
                GetColumnCount(w > 0 ? w : 800));
        };

        MasonryGrid.SizeChanged += MasonryGrid_SizeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnPageAppearingAsync();
    }

    private void MasonryGrid_SizeChanged(object? sender, EventArgs e)
    {
        var width = MasonryGrid.Width;
        if (width <= 0) return;
        if (Math.Abs(width - _lastWidth) < 5.0) return;
        _lastWidth = width;

        var newColumnCount = GetColumnCount(width);
        _viewModel.CurrentColumnCount = newColumnCount;

        if (newColumnCount == _lastColumnCount && _gridPopulated) return;
        _lastColumnCount = newColumnCount;
        _gridPopulated = false;

        _viewModel.DistributeIntoColumns(newColumnCount);
    }

    private static int GetColumnCount(double w) => w switch
    {
        < 600 => 2,
        < 900 => 3,
        < 1200 => 4,
        _ => 5
    };

    private void ClearMasonryGrid()
    {
        MasonryGrid.ColumnDefinitions.Clear();
        MasonryGrid.Children.Clear();
        _gridPopulated = false;
        _lastColumnCount = 0;
    }

    private void RebuildMasonryGrid()
    {
        var columns = _viewModel.Columns;
        if (columns.Count == 0) return;
        MasonryGrid.ColumnDefinitions.Clear();
        MasonryGrid.Children.Clear();

        // Build stacks WITHOUT binding items yet — let the grid do one layout pass
        // so stacks have correct widths before images measure against them
        for (int i = 0; i < columns.Count; i++)
        {
            MasonryGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var stack = new VerticalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Start };
            BindableLayout.SetItemTemplate(stack, BuildCardTemplate());
            Grid.SetColumn(stack, i);
            MasonryGrid.Children.Add(stack);
        }

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), () =>
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (MasonryGrid.Children[i] is VerticalStackLayout stack)
                    BindableLayout.SetItemsSource(stack, columns[i]);
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
            card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(18) };
            card.SetBinding(Border.StrokeProperty, new Binding("IsPinned", converter: new PinStrokeConverter()));
            card.SetBinding(Border.StrokeThicknessProperty, new Binding("IsPinned", converter: new PinStrokeThicknessConverter()));

            var tap = new TapGestureRecognizer();
            tap.SetBinding(TapGestureRecognizer.CommandProperty,
                new Binding("OpenDetailCommand",
                    source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestorBindingContext, typeof(WishlistViewModel))));
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, new Binding("."));
            card.GestureRecognizers.Add(tap);

            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += (s, e) =>
            {
                _ = card.ScaleTo(1.03, 160, Easing.CubicOut);
                card.Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#28000000")), Offset = new Point(0, 8), Radius = 24, Opacity = 1f };
            };
            pointer.PointerExited += (s, e) =>
            {
                _ = card.ScaleTo(1.0, 160, Easing.CubicOut);
                card.Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#14000000")), Offset = new Point(0, 2), Radius = 12, Opacity = 1f };
            };
            card.GestureRecognizers.Add(pointer);

            var root = new Grid();


            // PHOTO CARD
            var photoContainer = new Grid();
            photoContainer.SetBinding(Grid.IsVisibleProperty, "HasPhoto");
            var photo = new Controls.AspectLockedImage();
            photo.SetBinding(Image.SourceProperty, "PhotoPath");
            photo.Clip = new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry { Rect = new Rect(0, 0, 1000, 2000), CornerRadius = new CornerRadius(18) };
            photoContainer.Children.Add(photo);
            var gradient = new BoxView { VerticalOptions = LayoutOptions.Fill, HorizontalOptions = LayoutOptions.Fill };
            gradient.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                [
                    new GradientStop { Color = Colors.Transparent, Offset = 0.0f },
                    new GradientStop { Color = Color.FromArgb("#40000000"), Offset = 0.45f },
                    new GradientStop { Color = Color.FromArgb("#CC000000"), Offset = 1.0f },
                ]
            };
            gradient.Clip = new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry { Rect = new Rect(0, 0, 1000, 2000), CornerRadius = new CornerRadius(18) };
            photoContainer.Children.Add(gradient);
            var overlayText = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.End, Padding = new Thickness(14, 0, 14, 14) };
            var overlayCatRow = new HorizontalStackLayout { Spacing = 5 };
            var overlayEmoji = new Label { FontSize = 11 };
            overlayEmoji.SetBinding(Label.TextProperty, "PriorityEmoji");
            overlayCatRow.Children.Add(overlayEmoji);
            var overlayCatLabel = new Label { FontSize = 10, FontAttributes = FontAttributes.Bold, CharacterSpacing = 0.5, TextTransform = TextTransform.Uppercase, TextColor = Color.FromArgb("#CCffffff") };
            overlayCatLabel.SetBinding(Label.TextProperty, "CategoryLabel");
            overlayCatRow.Children.Add(overlayCatLabel);
            overlayText.Children.Add(overlayCatRow);
            var overlayName = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, LineBreakMode = LineBreakMode.WordWrap };
            overlayName.SetBinding(Label.TextProperty, "Name");
            overlayText.Children.Add(overlayName);
            var overlayPrice = new Label { FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            overlayPrice.SetBinding(Label.TextProperty, "PriceFormatted");
            overlayText.Children.Add(overlayPrice);
            var overlayBought = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#34c759") };
            overlayBought.SetBinding(Label.TextProperty, "StatusLabel");
            overlayBought.SetBinding(Label.IsVisibleProperty, "IsBought");
            overlayText.Children.Add(overlayBought);
            var overlayRegret = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold };
            overlayRegret.SetBinding(Label.TextProperty, "RegretLabel");
            overlayRegret.SetBinding(Label.IsVisibleProperty, "HasRegretRating");
            overlayRegret.SetBinding(Label.TextColorProperty,
                new Binding("IsWorthIt", converter: new BoolToAffordTextConverter()));
            overlayText.Children.Add(overlayRegret);
            var overlayTarget = new Label { FontSize = 11, TextColor = Color.FromArgb("#99ffffff") };
            overlayTarget.SetBinding(Label.TextProperty, "TargetMonthFormatted");
            overlayTarget.SetBinding(Label.IsVisibleProperty, new Binding("TargetMonthFormatted", converter: new StringToBoolConverter()));
            overlayText.Children.Add(overlayTarget);
            photoContainer.Children.Add(overlayText);
            root.Children.Add(photoContainer);


            // NO-PHOTO CARD
            var plainCard = new VerticalStackLayout { Spacing = 0 };
            plainCard.SetBinding(VisualElement.IsVisibleProperty, new Binding("HasPhoto", converter: new InverseBoolConverter()));
            var content = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(14, 18, 14, 14) };
            var catRow = new HorizontalStackLayout { Spacing = 5 };
            var emoji = new Label { FontSize = 11 };
            emoji.SetBinding(Label.TextProperty, "PriorityEmoji");
            catRow.Children.Add(emoji);
            var catLabel = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 0.5, TextTransform = TextTransform.Uppercase };
            catLabel.SetBinding(Label.TextProperty, "CategoryLabel");
            catLabel.SetDynamicResource(Label.TextColorProperty, "AccentColor");
            catRow.Children.Add(catLabel);
            content.Children.Add(catRow);
            var itemName = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.WordWrap };
            itemName.SetBinding(Label.TextProperty, "Name");
            itemName.SetDynamicResource(Label.TextColorProperty, "TextPrimary");
            content.Children.Add(itemName);
            var price = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
            price.SetBinding(Label.TextProperty, "PriceFormatted");
            price.SetDynamicResource(Label.TextColorProperty, "AccentColor");
            content.Children.Add(price);
            var coolingBanner = new Border { StrokeThickness = 0, Padding = new Thickness(6, 3) };
            coolingBanner.SetDynamicResource(Border.BackgroundColorProperty, "AccentColorAlpha");
            coolingBanner.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) };
            coolingBanner.SetBinding(Border.IsVisibleProperty, "ShowCoolingBanner");
            var coolingLabel = new Label { FontSize = 10 };
            coolingLabel.SetBinding(Label.TextProperty, "CoolingOffLabel");
            coolingLabel.SetDynamicResource(Label.TextColorProperty, "AccentColor");
            coolingBanner.Content = coolingLabel;
            content.Children.Add(coolingBanner);
            var staleBanner = new Border { StrokeThickness = 0, BackgroundColor = Color.FromArgb("#1FE8B84D"), Padding = new Thickness(6, 3) };
            staleBanner.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) };
            staleBanner.SetBinding(Border.IsVisibleProperty, "ShowStaleBanner");
            var staleLabel = new Label { FontSize = 10, TextColor = Color.FromArgb("#8a5a00") };
            staleLabel.SetBinding(Label.TextProperty, "StaleLabel");
            staleBanner.Content = staleLabel;
            content.Children.Add(staleBanner);
            var boughtLabel = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#34c759") };
            boughtLabel.SetBinding(Label.TextProperty, "StatusLabel");
            boughtLabel.SetBinding(Label.IsVisibleProperty, "IsBought");
            content.Children.Add(boughtLabel);
            var regretLabel = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold };
            regretLabel.SetBinding(Label.TextProperty, "RegretLabel");
            regretLabel.SetBinding(Label.IsVisibleProperty, "HasRegretRating");
            regretLabel.SetBinding(Label.TextColorProperty,
                new Binding("IsWorthIt", converter: new BoolToAffordTextConverter()));
            content.Children.Add(regretLabel);
            var targetLabel = new Label { FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
            targetLabel.SetBinding(Label.TextProperty, "TargetMonthFormatted");
            targetLabel.SetBinding(Label.IsVisibleProperty, new Binding("TargetMonthFormatted", converter: new StringToBoolConverter()));
            targetLabel.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
            content.Children.Add(targetLabel);
            plainCard.Children.Add(content);
            root.Children.Add(plainCard);

            // PINNED BADGE — overlaid top-left
            var pinnedBadge = new Border
            {
                StrokeThickness = 0,
                Padding = new Thickness(8, 4),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(10, 10, 0, 0),
            };
            pinnedBadge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(980) };
            pinnedBadge.SetDynamicResource(Border.BackgroundColorProperty, "AccentColor");
            pinnedBadge.SetBinding(Border.IsVisibleProperty, "IsPinned");
            pinnedBadge.Content = new Label { Text = "📌 Pinned", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            root.Children.Add(pinnedBadge);

            card.Content = root;
            return card;
        });
    }
}
