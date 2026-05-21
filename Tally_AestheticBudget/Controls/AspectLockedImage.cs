using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

namespace Tally_AestheticBudget.Controls;

/// <summary>
/// An Image that, once its source loads, locks its height to
/// (renderedWidth / naturalAspectRatio) so layout never reflows on rebind.
/// </summary>
public class AspectLockedImage : Image
{
    private double _aspectRatio = 0; // width / height of the source image

    public AspectLockedImage()
    {
        Aspect = Aspect.AspectFill;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Start;

        // Capture aspect ratio the moment the image loads
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Source))
                _aspectRatio = 0; // reset on source change

            if (e.PropertyName == nameof(Width) && Width > 0 && _aspectRatio == 0)
                TryReadAspect();
        };
    }

    private void TryReadAspect()
    {
        // Handler resolves the loaded image size
        if (Handler?.PlatformView == null) return;

#if WINDOWS
        if (Handler.PlatformView is Microsoft.UI.Xaml.Controls.Image img
            && img.Source is Microsoft.UI.Xaml.Media.Imaging.BitmapImage bmp
            && bmp.PixelWidth > 0 && bmp.PixelHeight > 0)
        {
            _aspectRatio = (double)bmp.PixelWidth / bmp.PixelHeight;
            InvalidateMeasure();
        }
#elif IOS || MACCATALYST
        if (Handler.PlatformView is UIKit.UIImageView iv
            && iv.Image?.Size is CoreGraphics.CGSize sz && sz.Height > 0)
        {
            _aspectRatio = sz.Width / sz.Height;
            InvalidateMeasure();
        }
#elif ANDROID
        if (Handler.PlatformView is Android.Widget.ImageView iv
            && iv.Drawable is Android.Graphics.Drawables.BitmapDrawable bd
            && bd.Bitmap?.Height > 0)
        {
            _aspectRatio = (double)bd.Bitmap.Width / bd.Bitmap.Height;
            InvalidateMeasure();
        }
#endif
    }

    protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
    {
        if (_aspectRatio > 0 && widthConstraint > 0 && !double.IsInfinity(widthConstraint))
        {
            var h = widthConstraint / _aspectRatio;
            return new SizeRequest(new Size(widthConstraint, h));
        }

        // Fallback before aspect is known
        return base.OnMeasure(widthConstraint, heightConstraint);
    }
}