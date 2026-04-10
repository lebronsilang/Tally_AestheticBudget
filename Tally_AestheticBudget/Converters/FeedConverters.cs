using Tally_AestheticBudget.Models;
using System.Globalization;

namespace Tally_AestheticBudget.Converters;

// Category label color — accent (#ff6b6b) matching .card-cat in your CSS
public class CategoryToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        // All categories use the accent color for their label, just like .card-cat { color: var(--accent) }
        => Color.FromArgb("#ff6b6b");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Category dot color — soft muted tones for the small dot beside the label
public class CategoryToDotColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ExpenseCategory cat ? cat switch
        {
            ExpenseCategory.Food => Color.FromArgb("#A8C5A0"),
            ExpenseCategory.Transport => Color.FromArgb("#A5BDD6"),
            ExpenseCategory.Shopping => Color.FromArgb("#D6A5C9"),
            ExpenseCategory.Health => Color.FromArgb("#F0B8A0"),
            ExpenseCategory.Fun => Color.FromArgb("#F5D08A"),
            ExpenseCategory.Grocery => Color.FromArgb("#B5C9A8"),
            _ => Color.FromArgb("#C8C6BE"),
        } : Colors.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Shows "Today", "Yesterday", or "d MMM" for older dates
public class DateToDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime date) return string.Empty;
        var today = DateTime.Today;
        if (date.Date == today) return "Today";
        if (date.Date == today.AddDays(-1)) return "Yesterday";
        return date.ToString("d MMM");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Active chip: accent fill (#ff6b6b), inactive: transparent — matches .nav-item.active
public class BoolToChipBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#ff6b6b")   // var(--accent)
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Active chip border: accent, inactive: soft gray border
public class BoolToChipBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#ff6b6b")
            : Color.FromArgb("#E0E0E5");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Active chip text: white, inactive: subtext gray — matches .nav-item.active { color: var(--accent) }
// We use white on the filled chip instead of accent-on-white for better contrast
public class BoolToChipTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Colors.White
            : Color.FromArgb("#6e6e73");   // var(--subtext)

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Bold when active, normal when inactive
public class BoolToFontAttributesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontAttributes.Bold : FontAttributes.None;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Flips a bool — used to show CollectionView when HasNoEntries is false
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Gives cards without a photo more top padding, matching:

public class PhotoToPaddingConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? new Thickness(14, 12, 14, 6)
            : new Thickness(14, 18, 14, 6);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Shows different subtitle for add vs edit mode
public class BoolToEditSubtitleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Update your expense" : "Log what you spent";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── Add both classes to the bottom of FeedConverters.cs ─────────────────────

// Progress bar color — accent normally, red when over limit
// Matches .progress-fill.over { background: #ff3b30 } in your CSS
public class BoolToProgressColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#ff3b30")   // over limit — red
            : Color.FromArgb("#ff6b6b");  // normal — accent coral

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Converts a 0.0–1.0 progress percent into a pixel width for the progress fill bar.
// The bar container is the full card width minus padding (roughly 280px on most screens).
// We multiply percent × 280 to get the fill width in pixels.
public class PercentToWidthConverter : IValueConverter
{
    private const double MaxWidth = 280.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double d ? d * MaxWidth : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── Add these three classes to the bottom of FeedConverters.cs ───────────────

// Fades checked items to 0.45 opacity — matches .grocery-item.checked { opacity: 0.5 }
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 0.45 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Strikethrough on checked item names — matches .grocery-item.checked .grocery-name
public class BoolToStrikethroughConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TextDecorations.Strikethrough : TextDecorations.None;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Shows empty state when count is 0
public class ZeroToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int count && count == 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Shows a label only when string is not null/empty
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}