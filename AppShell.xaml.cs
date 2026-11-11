
using System.Diagnostics;

namespace RoyalPlay;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register all navigation routes once.
        Routing.RegisterRoute(
            nameof(Pages.RegisterPage),
            typeof(Pages.RegisterPage));

        Routing.RegisterRoute(
            nameof(Pages.PokerPage),
            typeof(Pages.PokerPage));

        Routing.RegisterRoute(
            nameof(Pages.BlackjackPage),
            typeof(Pages.BlackjackPage));

        Routing.RegisterRoute(
            nameof(Pages.SettingsPage),
            typeof(Pages.SettingsPage));
    }

    // Keeps the original MAUI template code compatible.
    public static Task DisplayToastAsync(string message)
    {
        Debug.WriteLine(message);
        return Task.CompletedTask;
    }
}
