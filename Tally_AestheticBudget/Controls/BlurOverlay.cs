namespace Tally_AestheticBudget.Controls;

/// <summary>
/// Full-screen frosted glass overlay using a semi-transparent light tint.
/// Fills its parent Grid cell to cover the background content.
/// </summary>
public class BlurOverlay : BoxView
{
    public BlurOverlay()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        Color = Color.FromArgb("#CCF0F0F0");
    }
}
