using Tally_AestheticBudget.Services;

namespace Tally_AestheticBudget;

public partial class App : Application
{
    public static string CurrentAccent { get; set; } = "#ff6b6b";

    public App(IThemeService themeService)
    {
        InitializeComponent();          // ← resources are ready after this
        themeService.ApplyOnStartup();  // ← now safe to write to Resources
        MainPage = new AppShell();
    }
}