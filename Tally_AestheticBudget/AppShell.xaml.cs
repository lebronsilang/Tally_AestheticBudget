using Tally_AestheticBudget.Converters;
using Tally_AestheticBudget.Views;

namespace Tally_AestheticBudget;

public partial class AppShell : Shell
{
    private readonly Dictionary<string, (Border tab, Label label)> _tabs;
    private string _currentRoute = "FeedPage";
    private readonly Services.HeaderState _header;

    public AppShell(Services.HeaderState header)
    {
        InitializeComponent();
        _header = header;
        HeaderInfo.BindingContext = header;

        Routing.RegisterRoute(nameof(AddExpensePage), typeof(AddExpensePage));

        _tabs = new()
        {
            ["FeedPage"] = (TabFeed, TabFeedLabel),
            ["BudgetPage"] = (TabBudget, TabBudgetLabel),
            ["WishlistPage"] = (TabWishlist, TabWishlistLabel),
            ["GroceryPage"] = (TabGrocery, TabGroceryLabel),
            ["ThemesPage"] = (TabThemes, TabThemesLabel),
            ["SettingsPage"] = (TabSettings, TabSettingsLabel),
        };
        SetActiveTab("FeedPage");

        Navigated += (s, e) =>
        {
            var route = CurrentState.Location.OriginalString.TrimStart('/');
            var page = _tabs.Keys.FirstOrDefault(k => route.Contains(k));
            if (page is not null) SetActiveTab(page);

            // Feed & Budget own their filter label; every other page shows the brand.
            if (page is not ("FeedPage" or "BudgetPage"))
                _header.ShowBrand();
        };
    }

    private void SetActiveTab(string route)
    {
        _currentRoute = route;
        var accent = Color.FromArgb(App.CurrentAccent);

        foreach (var (key, (tab, label)) in _tabs)
        {
            bool isActive = key == route;
            tab.BackgroundColor = isActive ? accent.WithAlpha(0.12f) : Colors.Transparent;
            label.TextColor = isActive ? accent : ThemeColors.Get("TextSecondary", "#6e6e73");
            label.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    internal void OnTabTapped(object sender, TappedEventArgs e)
    {
        var route = e.Parameter?.ToString();
        if (route is null) return;
        _ = GoToAsync($"//{route}");
        SetActiveTab(route);
    }

    internal void OnAddTapped(object sender, TappedEventArgs e)
    {
        if (Current?.CurrentPage is Views.FeedPage feedPage)
        {
            var vm = feedPage.BindingContext as ViewModels.FeedViewModel;
            vm?.OpenAddModalCommand.Execute(null);
        }
    }

    public void RefreshAccent()
    {
        SetActiveTab(_currentRoute);
    }
}