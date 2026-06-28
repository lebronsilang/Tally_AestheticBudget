namespace Tally_AestheticBudget.Behaviors;

/// <summary>
/// Reusable tactile "press" animation — scales the attached element down slightly
/// on pointer-down and back up on release, matching the feel of the Wishlist
/// detail panel's Status/Worth-It toggle buttons. Drop it on any tappable
/// Border/Grid/Label "fake button" via:
///   &lt;Border.Behaviors&gt;
///       &lt;behaviors:PressScaleBehavior /&gt;
///   &lt;/Border.Behaviors&gt;
/// </summary>
public class PressScaleBehavior : Behavior<View>
{
    public static readonly BindableProperty ScaleDownProperty =
        BindableProperty.Create(nameof(ScaleDown), typeof(double), typeof(PressScaleBehavior), 0.96);

    public double ScaleDown
    {
        get => (double)GetValue(ScaleDownProperty);
        set => SetValue(ScaleDownProperty, value);
    }

    private PointerGestureRecognizer? _pointer;
    private View? _target;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _target = bindable;

        _pointer = new PointerGestureRecognizer();
        _pointer.PointerPressed += OnPressed;
        _pointer.PointerReleased += OnReleased;
        bindable.GestureRecognizers.Add(_pointer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        base.OnDetachingFrom(bindable);
        if (_pointer is not null)
        {
            _pointer.PointerPressed -= OnPressed;
            _pointer.PointerReleased -= OnReleased;
            bindable.GestureRecognizers.Remove(_pointer);
        }
        _pointer = null;
        _target = null;
    }

    private void OnPressed(object? sender, PointerEventArgs e)
    {
        if (_target is not null)
            _ = _target.ScaleTo(ScaleDown, 70, Easing.CubicOut);
    }

    private void OnReleased(object? sender, PointerEventArgs e)
    {
        if (_target is not null)
            _ = _target.ScaleTo(1.0, 90, Easing.CubicOut);
    }
}
