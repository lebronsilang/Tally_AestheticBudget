using CommunityToolkit.Mvvm.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

namespace Tally_AestheticBudget.Services;

/// <summary>
/// Drives the shell's upper-left header. Shows the brand ("tally ✦") by default,
/// or the active page's filter label (e.g. "May 2026") when a filter page sets one.
/// </summary>
public partial class HeaderState : ObservableObject
{
    [ObservableProperty]
    private string _text = "tally";

    // Hide the ✦ sparkle when a filter label is showing, for a cleaner look.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBrandMark))]
    private bool _isBrand = true;

    public bool ShowBrandMark => IsBrand;

    public void ShowFilter(string label)
    {
        Text = string.IsNullOrWhiteSpace(label) ? "tally" : label;
        IsBrand = string.IsNullOrWhiteSpace(label);
    }

    public void ShowBrand()
    {
        Text = "tally";
        IsBrand = true;
    }
}