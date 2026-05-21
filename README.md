# Tally — Your Aesthetic Budget

**Because budgeting shouldn't feel like a chore.** Tally is a personal finance app built with .NET MAUI that makes tracking money feel less dreadful. Tally combines a visual, Pinterest-style feed with real budgeting tools, a grocery tracker, and a dream board, all wrapped in a themeable, minimal interface. Originally, this app was built with the sole intention of doing a school project. However, the idea came from a real (and painful) experience with finance apps and the clinical aesthetic they have. 

**Platform:** Windows Desktop (v1), Android planned post-v1  
**Status:** Minimum viable product complete. 

---

## Screenshots

![Feed screen](assets/feed.png)

---

## Features

**Feed (Spending Journal)**  
Expenses appear as cards in an adaptive masonry grid. Cards with a photo get a full-bleed image with a gradient overlay. Cards without a photo use themed styling. The grid adjusts from 2 to 5 columns based on window width. Filter by all time, a specific month, or the current year.

**Add / Edit Expense**  
A single page handles both add and edit flows via a query property (`ExpenseId`). Supports category selection, amount, date, notes, and an optional photo. Categories are a fixed enum (Food, Transport, Shopping, Health, Entertainment, and more).

**Budget Overview**  
Set a spending limit per category. Each category card shows a progress bar that turns red when you go over. Edit limits inline without leaving the page.

**Grocery Tracker**  
Group-based grocery lists with estimated prices per item. A budget bar shows how much of your grocery budget you have left. Check off items as you shop; clear checked items in bulk.

**Dream Board (Wishlist)**  
A wish list with Priority tags (Need, Want, Someday), a cooling-off reminder system, and a status flow from Planned to Bought. Bought items can log a regret rating. Pinned items float to the top. Wishlist items can be converted directly into expenses.

**Themes**  
Six preset themes (Clean Minimal, Sakura Bloom, Mocha Latte, Ocean Mist, Forest Dew, Midnight) plus a custom palette picker for background, accent, card, and text colors. Theme changes rebuild the shell immediately so every screen updates at once.

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI framework | .NET MAUI (targeting `net10.0-windows`) |
| Language | C# |
| Architecture | MVVM with dependency injection |
| Database | SQLite via `sqlite-net-pcl` |
| ViewModel toolkit | `CommunityToolkit.Mvvm` |
| Fonts | DM Serif Display (headings), DM Sans (body) |
| Icons | Phosphor Icons (SVG) |
| Theme storage | `Microsoft.Maui.Storage.Preferences` |

---

## Project Structure

```
Tally_AestheticBudget/
├── Models/
│   ├── Entities.cs          # SQLite table definitions
│   ├── FeedModels.cs        # Feed card view models
│   ├── BudgetModels.cs
│   ├── GroceryModels.cs
│   ├── WishModels.cs
│   └── ThemeModels.cs
├── ViewModels/
│   ├── FeedViewModel.cs
│   ├── AddExpenseViewModel.cs
│   ├── BudgetViewModel.cs
│   ├── GroceryViewModel.cs
│   ├── WishlistViewModel.cs
│   ├── ThemesViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── FeedPage.xaml / .cs
│   ├── AddExpensePage.xaml / .cs
│   ├── BudgetPage.xaml / .cs
│   ├── GroceryPage.xaml / .cs
│   ├── WishlistPage.xaml / .cs
│   ├── ThemesPage.xaml / .cs
│   └── SettingsPage.xaml / .cs
├── Services/
│   ├── DatabaseService.cs
│   ├── ExpenseService.cs    # IExpenseService
│   ├── BudgetService.cs     # IBudgetService
│   ├── GroceryService.cs    # IGroceryService
│   ├── WishService.cs       # IWishService
│   ├── ThemeService.cs      # IThemeService
│   └── SettingsService.cs   # ISettingsService
├── Converters/
│   └── FeedConverters.cs
├── Controls/
│   ├── AspectLockedImage.cs
│   └── BlurOverlay.cs
├── App.xaml / App.xaml.cs
├── AppShell.xaml / .cs
└── MauiProgram.cs           # DI registration
```

---

## Database Schema

Five SQLite tables, created on first launch and never dropped:

| Table | Purpose |
|---|---|
| `expenses` | All expense entries |
| `budgets` | Per-category spending limits |
| `grocery_groups` | Named grocery lists |
| `grocery_items` | Items within a grocery group |
| `wish_items` | Dream board entries |

The database lives in `FileSystem.AppDataDirectory` (`budget.db`), which resolves to the app's private data folder on both Windows and Android.

---

## Architecture Notes

**Dependency injection** is configured in `MauiProgram.cs`. Services are registered as singletons; ViewModels and Pages as transients.

**Theme system** works by writing color values directly into `Application.Current.Resources` at runtime. When a theme changes, `ThemeService.ReloadShell()` replaces `MainPage` with a fresh `AppShell` instance so every page picks up the new colors. `App.CurrentAccent` must be set before `ReloadShell()` is called.

**Masonry grid** on the Feed and Wishlist pages is built programmatically in the code-behind (`FeedPage.xaml.cs`, `WishlistPage.xaml.cs`). This is intentional — layout construction logic that depends on measured widths does not belong in a ViewModel. The XAML file for those pages contains only a named `MasonryGrid` container.

**Add and Edit expense** share one page. The route passes an `ExpenseId` query parameter; `IsEditMode` on the ViewModel drives the page title, subtitle, and button label.

**Theme storage** uses `Preferences`, not SQLite, because themes are app settings, not user data.

---

## Getting Started

### Prerequisites

- Visual Studio 2022 or later (Windows)
- .NET 10 SDK
- .NET MAUI workload (`dotnet workload install maui`)

### Run (Windows, unpackaged)

1. Clone the repo.
2. Open `Tally_AestheticBudget.sln` in Visual Studio.
3. Set the target to `Windows Machine`.
4. Press **F5**.

The SQLite database is created automatically on first launch. No migrations needed.

### NuGet packages

```
sqlite-net-pcl
SQLitePCLRaw.bundle_green
CommunityToolkit.Mvvm
```

These are already listed in the `.csproj`. A NuGet restore runs automatically on build.

---

## Theming

All color resources use semantic keys so pages never reference hex values directly:

| Key | Role |
|---|---|
| `PageBackground` | Page background |
| `CardBackground` | Card and nav bar background |
| `CardBorder` | Card stroke color |
| `TextPrimary` | Main text |
| `TextSecondary` | Subtitles and hints |
| `AccentColor` | Buttons, highlights, active states |

To change the accent at runtime, set `App.CurrentAccent` and call `ThemeService.ApplyColorsToResources()`.

---

## Roadmap

- [ ] Midnight theme text contrast fixes
- [ ] Quantity deduction on grocery items
- [ ] option to display tab bar icons in the navigation bar with or without text label
- [ ] Per-month themes (requires `monthly_themes` DB table)
- [ ] Grocery photo and note before Buy Checked
- [ ] More preferences/customisations on settings page
- [ ] Windows app packaging (`ExtendsContentIntoTitleBar`, MSIX)
- [ ] Android / mobile build (TargetFrameworks, touch layout adjustments)

---

## Note

Initially built for our Software Project Management course. However, the concept has been something personal to the lead developer so the app development will continue past the semester. 

---

## Team
## Team

| Name | Role |
|---|---|
| Le Bron Silang | Lead architect — system design, database, services, MVVM, core features |
| Zoe Anasco | Lead frontend designer and developer — UI, aesthetics, visual polish |
| Arielle Atim | Backend contributor and QA — feature support and testing |