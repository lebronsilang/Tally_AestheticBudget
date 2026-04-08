using Tally_AestheticBudget.Services;
using Tally_AestheticBudget.ViewModels;
using Tally_AestheticBudget.Views;
using Microsoft.Extensions.Logging;
using Tally_AestheticBudget;
using Tally_AestheticBudget.Services;

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
                // Default fonts that come with the MAUI template
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                // Add these later when you download DM Sans / DM Serif Display
                // fonts.AddFont("DMSerifDisplay-Regular.ttf", "DisplayFont");
                // fonts.AddFont("DMSans-Regular.ttf", "BodyFont");
            });

        // ── Services ──────────────────────────────────────────────────────────
        // AddSingleton = one instance for the whole app lifetime (good for DB)
        // AddTransient = a fresh instance every time it's requested (good for ViewModels)

        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<IExpenseService, ExpenseService>();

        // ── ViewModels ────────────────────────────────────────────────────────────
        builder.Services.AddTransient<FeedViewModel>();
        builder.Services.AddTransient<AddExpenseViewModel>();

        // ── Pages ─────────────────────────────────────────────────────────────────
        builder.Services.AddTransient<FeedPage>();
        builder.Services.AddTransient<AddExpensePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}