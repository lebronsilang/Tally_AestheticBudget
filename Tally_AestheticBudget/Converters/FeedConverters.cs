using Tally_AestheticBudget.Models;
using System.Globalization;

namespace Tally_AestheticBudget.Converters;

// Maps ExpenseCategory → a muted accent color (Clean Minimal palette)
public class CategoryToColorConverter : IValueConverter
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

// true → dark chip fill, false → transparent
public class BoolToChipBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#1A1A18") : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// true → dark border, false → light border
public class BoolToChipBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#1A1A18") : Color.FromArgb("#DDDDD8");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// true → white text, false → gray text
public class BoolToChipTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Colors.White : Color.FromArgb("#5F5E5A");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// true → Bold, false → None
public class BoolToFontAttributesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontAttributes.Bold : FontAttributes.None;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Flips a bool — used to show the CollectionView when HasNoEntries is false
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}