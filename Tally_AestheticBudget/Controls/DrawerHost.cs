using System.Windows.Input;

namespace Tally_AestheticBudget.Controls;

/// <summary>
/// A non-modal slide-in side panel shared by every "add / edit" surface
/// (Feed expense, Grocery item, Wishlist entry).
///
/// Unlike a popup, the drawer does NOT cover or dim the page. The body and the
/// drawer are two columns of a grid: the body is a Star column, the drawer is an
/// Absolute column animated between 0 and <see cref="DrawerWidth"/>. As the drawer
/// grows, the body's Star column shrinks to compensate, so the page reflows into
/// the remaining width and stays fully visible and interactive the whole time.
///
/// The drawer panel is anchored to the outer edge and clipped, so it reads as a
/// genuine slide-in from the chosen side rather than a reveal.
///
/// XAML:
///   &lt;controls:DrawerHost IsOpen="{Binding IsPanelOpen}"
///                        OpenFromLeft="{Binding PanelOnLeft}"
///                        DrawerWidth="380"&gt;
///       &lt;controls:DrawerHost.BodyContent&gt;   ... the page ...   &lt;/controls:DrawerHost.BodyContent&gt;
///       &lt;controls:DrawerHost.DrawerContent&gt; ... the form ...   &lt;/controls:DrawerHost.DrawerContent&gt;
///   &lt;/controls:DrawerHost&gt;
///
/// Feasibility: column-width animation works on every MAUI target. It is intended
/// for desktop (Windows/macOS); on a narrow phone a fixed 380 drawer would swamp
/// the screen, so set DrawerWidth to a fraction of the page width there.
/// </summary>
public class DrawerHost : ContentView
{
    private readonly Grid _root;
    private readonly ContentView _bodyHost = new();
    private readonly Grid _drawerClip;          // clips the panel while the column is narrower than the panel
    private readonly ContentView _drawerPanel;  // fixed-width; holds DrawerContent
    private readonly ColumnDefinition _bodyCol = new() { Width = GridLength.Star };
    private readonly ColumnDefinition _drawerCol = new() { Width = new GridLength(0, GridUnitType.Absolute) };
    private readonly TapGestureRecognizer _bodyTap = new();

    public DrawerHost()
    {
        _drawerPanel = new ContentView
        {
            WidthRequest = 360,
            HorizontalOptions = LayoutOptions.End,  // anchored to the outer edge → slides in
            VerticalOptions = LayoutOptions.Fill,
        };

        _drawerClip = new Grid { IsClippedToBounds = true };
        _drawerClip.Add(_drawerPanel);

        _root = new Grid();
        _root.Add(_bodyHost);
        _root.Add(_drawerClip);

        _bodyTap.Tapped += (_, _) =>
        {
            if (IsOpen && DismissOnBodyTap && CloseCommand?.CanExecute(null) == true)
                CloseCommand.Execute(null);
        };

        ApplySideLayout();
        Content = _root;
    }

    // ── Slots ──────────────────────────────────────────────────────────────────

    public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(
        nameof(BodyContent), typeof(View), typeof(DrawerHost), null,
        propertyChanged: (b, _, n) => ((DrawerHost)b)._bodyHost.Content = n as View);

    public View BodyContent
    {
        get => (View)GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }

    public static readonly BindableProperty DrawerContentProperty = BindableProperty.Create(
        nameof(DrawerContent), typeof(View), typeof(DrawerHost), null,
        propertyChanged: (b, _, n) => ((DrawerHost)b)._drawerPanel.Content = n as View);

    public View DrawerContent
    {
        get => (View)GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    // ── State / configuration ────────────────────────────────────────────────────

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(DrawerHost), false,
        propertyChanged: (b, _, n) => ((DrawerHost)b).OnIsOpenChanged((bool)n));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly BindableProperty OpenFromLeftProperty = BindableProperty.Create(
        nameof(OpenFromLeft), typeof(bool), typeof(DrawerHost), false,
        propertyChanged: (b, _, _) => ((DrawerHost)b).ApplySideLayout());

    public bool OpenFromLeft
    {
        get => (bool)GetValue(OpenFromLeftProperty);
        set => SetValue(OpenFromLeftProperty, value);
    }

    public static readonly BindableProperty DrawerWidthProperty = BindableProperty.Create(
        nameof(DrawerWidth), typeof(double), typeof(DrawerHost), 360d,
        propertyChanged: (b, _, n) => ((DrawerHost)b).OnDrawerWidthChanged((double)n));

    public double DrawerWidth
    {
        get => (double)GetValue(DrawerWidthProperty);
        set => SetValue(DrawerWidthProperty, value);
    }

    public static readonly BindableProperty AnimationLengthProperty = BindableProperty.Create(
        nameof(AnimationLength), typeof(uint), typeof(DrawerHost), (uint)220);

    public uint AnimationLength
    {
        get => (uint)GetValue(AnimationLengthProperty);
        set => SetValue(AnimationLengthProperty, value);
    }

    /// <summary>When true, tapping the page body while the drawer is open runs
    /// <see cref="CloseCommand"/>. Off by default: in a reflow drawer the page stays
    /// interactive, so a stray tap closing a half-filled form is usually unwanted.</summary>
    public static readonly BindableProperty DismissOnBodyTapProperty = BindableProperty.Create(
        nameof(DismissOnBodyTap), typeof(bool), typeof(DrawerHost), false,
        propertyChanged: (b, _, _) => ((DrawerHost)b).ApplyBodyTap());

    public bool DismissOnBodyTap
    {
        get => (bool)GetValue(DismissOnBodyTapProperty);
        set => SetValue(DismissOnBodyTapProperty, value);
    }

    public static readonly BindableProperty CloseCommandProperty = BindableProperty.Create(
        nameof(CloseCommand), typeof(ICommand), typeof(DrawerHost), null);

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    // ── Internals ────────────────────────────────────────────────────────────────

    private void OnDrawerWidthChanged(double w)
    {
        _drawerPanel.WidthRequest = w;
        if (IsOpen) _drawerCol.Width = new GridLength(w, GridUnitType.Absolute);
    }

    private void ApplySideLayout()
    {
        _drawerPanel.HorizontalOptions = OpenFromLeft ? LayoutOptions.Start : LayoutOptions.End;

        _root.ColumnDefinitions.Clear();
        if (OpenFromLeft)
        {
            _root.ColumnDefinitions.Add(_drawerCol);
            _root.ColumnDefinitions.Add(_bodyCol);
            Grid.SetColumn(_drawerClip, 0);
            Grid.SetColumn(_bodyHost, 1);
        }
        else
        {
            _root.ColumnDefinitions.Add(_bodyCol);
            _root.ColumnDefinitions.Add(_drawerCol);
            Grid.SetColumn(_bodyHost, 0);
            Grid.SetColumn(_drawerClip, 1);
        }
    }

    private void ApplyBodyTap()
    {
        _bodyHost.GestureRecognizers.Remove(_bodyTap);
        if (DismissOnBodyTap)
            _bodyHost.GestureRecognizers.Add(_bodyTap);
    }

    private void OnIsOpenChanged(bool open)
    {
        this.AbortAnimation("drawer");

        double from = _drawerCol.Width.Value;       // always Absolute here
        double to = open ? DrawerWidth : 0d;

        if (Math.Abs(from - to) < 0.5)
        {
            _drawerCol.Width = new GridLength(to, GridUnitType.Absolute);
            return;
        }

        var anim = new Animation(
            v => _drawerCol.Width = new GridLength(v, GridUnitType.Absolute),
            from, to);

        anim.Commit(this, "drawer", 16, AnimationLength,
            open ? Easing.CubicOut : Easing.CubicIn);
    }
}