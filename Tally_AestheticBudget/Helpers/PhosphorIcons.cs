namespace Tally_AestheticBudget.Helpers;

public static class PhosphorIcons
{
    // ── Navigation (already used in AppShell — kept here as the single source of truth)
    public const string Receipt = "\uE3EC";
    public const string Wallet = "\uE68A";
    public const string Heart = "\uE2A8";
    public const string Basket = "\uE964";
    public const string Palette = "\uE6C8";
    public const string Gear = "\uE270";

    // ── Expense categories ─
    public const string Food = "\uE262"; // fork-knife
    public const string Transport = "\uE112"; // car
    public const string Shopping = "\uE416"; // shopping-bag
    public const string Health = "\uE2AC"; // heartbeat
    public const string Fun = "\uE81A"; // confetti
    public const string Grocery = "\uE964"; // basket (shared with nav)
    public const string Default = "\uE478"; // tag

    // ── Actions ───
    public const string Add = "\uE3D4"; // plus
    public const string Edit = "\uE3B4"; // pencil-simple
    public const string Delete = "\uE4A6"; // trash
    public const string Confirm = "\uE182"; // check
    public const string Cancel = "\uE4F6"; // x
    public const string ThumbsUp = "\uE182"; // reuse check glyph \u2014 "worth it" approval
    public const string ThumbsDown = "\uE4F6"; // reuse x glyph \u2014 "regret" rejection
    public const string Note = "\uE34C"; // note-pencil

    // ── Media / capture 
    public const string Camera = "\uE10E"; // camera
    public const string Image = "\uE2CA"; // image

    // ── Status / indicators 
    public const string Warning = "\uE4E0"; // warning (triangle)
    public const string WarningCircle = "\uE4E2"; // warning-circle
    public const string Info = "\uE2CE"; // info
    public const string CheckCircle = "\uE184"; // check-circle
    public const string Sparkle = "\uE6A2"; // sparkle

    // ── Empty states / misc 
    public const string Tray = "\uE4AA"; // tray (empty state)
    public const string Calendar = "\uE10A"; // calendar-blank
    public const string CaretDown = "\uE136"; // caret-down
    public const string CaretRight = "\uE13A"; // caret-right

    public const string Coins = "\uE190";           // "coins"            (Display Currency)
    public const string MagnifyingGlass = "\uE320"; // "magnifying-glass" (currency search)
    public const string Calculator = "\uE108";      // "calculator"       (Allot Remaining)
    public const string Hourglass = "\uE2BE";       // "hourglass-medium" (Cooling-off)
    public const string Eye = "\uE228";             // "eye"              (Stale Reminder)
    public const string Keyboard = "\uE300";        // "keyboard"         (Item 4 — Shortcuts section)
    public const string ChartBar = "\uE154"; // "chart-bar"   
    public const string PushPin = "\uE3D6"; // "push-pin"    

    // ── Photo source chooser 
    public const string FolderOpen = "\uE256 "; // "folder-open"    
    public const string Globe = "\uE27A"; // "globe"          
    public const string ClipboardText = "\uE196"; // "clipboard-text" 
    public const string Link = "\uE2E6"; // "link"           
}