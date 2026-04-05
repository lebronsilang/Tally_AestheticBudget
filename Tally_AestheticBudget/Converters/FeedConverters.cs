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
        // hasPhoto = true  → photo sits above, so normal padding: 12,14,14
        // hasPhoto = false → no photo, needs more top breathing room: 18,14,14
        => value is true
            ? new Thickness(14, 12, 14, 6)
            : new Thickness(14, 18, 14, 6);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}