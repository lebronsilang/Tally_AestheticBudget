using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget;

public partial class App : Application
{
    public static string CurrentAccent { get; set; } = "#ff6b6b";

    private readonly HeaderState _header;

    public App(IThemeService themeService, HeaderState header)
    {
        InitializeComponent();
        _header = header;

        // Apply the persisted theme to Application.Resources before any window
        // is realized, so the first frame is already correctly coloured.
        themeService.ApplyOnStartup();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell(_header))
        {
            Title = "Tally",           
            MinimumWidth = 920,
            MinimumHeight = 620,
        };

        return window;
    }
}