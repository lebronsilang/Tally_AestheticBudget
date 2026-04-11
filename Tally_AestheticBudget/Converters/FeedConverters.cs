using Tally_AestheticBudget.Models;
using System.Globalization;

namespace Tally_AestheticBudget.Converters;

// Category label color — accent matching .card-cat in your CSS
public class CategoryToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Color.FromArgb(App.CurrentAccent);

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

// ── FIXED: reads App.CurrentAccent instead of hardcoded #ff6b6b ──────────────
// Active chip: accent fill, inactive: transparent — matches .nav-item.active
public class BoolToChipBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb(App.CurrentAccent)
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── FIXED: reads App.CurrentAccent instead of hardcoded #ff6b6b ──────────────
// Active chip border: accent, inactive: soft gray border
public class BoolToChipBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb(App.CurrentAccent)
            : Color.FromArgb("#E0E0E5");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Active chip text: white on filled chip, subtext gray when inactive
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

// ── FIXED: reads App.CurrentAccent for the normal (non-over) state ────────────
// Progress bar color — accent normally, red when over limit
// Matches .progress-fill.over { background: #ff3b30 } in your CSS
public class BoolToProgressColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#ff3b30")           // over limit — always red
            : Color.FromArgb(App.CurrentAccent);  // normal — current theme accent

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── FIXED: reads App.CurrentAccent instead of hardcoded #ff6b6b ──────────────
// Converts a 0.0–1.0 progress percent into a pixel width for the progress fill bar.
// The bar container is the full card width minus padding (roughly 280px on most screens).
// Converts a 0.0–1.0 progress percent into a pixel width for the progress fill bar.
// Uses the actual screen width minus known padding so the bar scales correctly
// on all screen sizes (phone, tablet, Windows desktop).
public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double percent) return 0.0;

        // Get screen width in device-independent units (dp)
        var screenWidth = DeviceDisplay.MainDisplayInfo.Width
                          / DeviceDisplay.MainDisplayInfo.Density;

        // Subtract: page horizontal padding (20+20) + card horizontal padding (18+18)
        var usableWidth = screenWidth - 76;

        // Clamp so the bar never goes negative or overflows
        usableWidth = Math.Max(usableWidth, 100);

        return percent * usableWidth;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Fades checked items to 0.45 opacity — matches .grocery-item.checked { opacity: 0.5 }
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 0.45 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Strikethrough on checked item names
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

// Afford indicator background — green if can afford, red if not
public class BoolToAffordBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#1234c759")   // green tint
            : Color.FromArgb("#1Fff3b30");  // red tint

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Afford indicator text color
public class BoolToAffordTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? Color.FromArgb("#1a7a40")   // green text
            : Color.FromArgb("#c0392b");  // red text

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Status toggle — Planned button background
public class StatusToPlannedBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is WishStatus.Planned
            ? Color.FromArgb("#f5f5f7")
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Status toggle — Planned button text color
public class StatusToPlannedTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is WishStatus.Planned
            ? Color.FromArgb("#1d1d1f")
            : Color.FromArgb("#6e6e73");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Status toggle — Bought button background
public class StatusToBoughtBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is WishStatus.Bought
            ? Color.FromArgb("#1234c759")
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Status toggle — Bought button text color
public class StatusToBoughtTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is WishStatus.Bought
            ? Color.FromArgb("#1a7a40")
            : Color.FromArgb("#6e6e73");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Pin button label — toggles between pin and unpin
public class BoolToPinLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "⭐ Unpin" : "⭐ Pin as Top Dream";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Checks if the current ExpenseCategory matches a given category name parameter
public class CategoryMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ExpenseCategory current) return false;
        if (parameter is not string target) return false;
        return Enum.TryParse<ExpenseCategory>(target, out var t) && current == t;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ── FIXED: reads App.CurrentAccent instead of hardcoded #ff6b6b ──────────────
// Highlights the active theme card with the current accent border.
public class ThemeActiveBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var themeId = value as string;
        var activeId = Preferences.Get("active_theme", "default");
        return themeId == activeId
            ? Color.FromArgb(App.CurrentAccent)  // active — current accent
            : Color.FromArgb("#E8E8ED");          // inactive — subtle border
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}