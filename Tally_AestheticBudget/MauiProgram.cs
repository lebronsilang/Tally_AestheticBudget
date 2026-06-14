using Tally_AestheticBudget.Services;
using Tally_AestheticBudget.ViewModels;
using Tally_AestheticBudget.Views;
using Microsoft.Extensions.Logging;
using Tally_AestheticBudget;

namespace Tally_AestheticBudget;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("DMSans-Regular.ttf", "BodyFont");
                fonts.AddFont("DMSerifDisplay-Regular.ttf", "DisplayFont");
            });


        //Services
        // AddSingleton = one instance for the whole app lifetime (good for DB)
        // AddTransient = a fresh instance every time it's requested (good for ViewModels)

        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<DataChangedService>();
        builder.Services.AddSingleton<HeaderState>();
        builder.Services.AddSingleton<IExpenseService, ExpenseService>();
        builder.Services.AddSingleton<IBudgetService, BudgetService>();
        builder.Services.AddSingleton<IGroceryService, GroceryService>();
        builder.Services.AddSingleton<IWishService, WishService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();


        //ViewModels
        builder.Services.AddTransient<FeedViewModel>();
        builder.Services.AddTransient<AddExpenseViewModel>();
        builder.Services.AddTransient<BudgetViewModel>();
        builder.Services.AddTransient<GroceryViewModel>();
        builder.Services.AddTransient<WishlistViewModel>();
        builder.Services.AddTransient<ThemesViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        //Pages
        builder.Services.AddTransient<FeedPage>();
        builder.Services.AddTransient<AddExpensePage>();
        builder.Services.AddTransient<BudgetPage>();
        builder.Services.AddTransient<GroceryPage>();
        builder.Services.AddTransient<WishlistPage>();
        builder.Services.AddTransient<ThemesPage>();
        builder.Services.AddTransient<SettingsPage>();


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}