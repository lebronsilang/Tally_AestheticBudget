using Tally_AestheticBudget.Services;


namespace Tally_AestheticBudget;

public partial class App : Application
{

    public static string CurrentAccent { get; set; } = "#ff6b6b";

    public App(IThemeService themeService, HeaderState header)
    {
        InitializeComponent();
        themeService.ApplyOnStartup();
        MainPage = new AppShell(header);
    }
}

