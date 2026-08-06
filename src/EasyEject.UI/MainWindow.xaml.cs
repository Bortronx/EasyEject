using EasyEject.Core.Contracts;
using EasyEject.UI.Services;
using EasyEject.UI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Serilog;

namespace EasyEject.UI;

/// <summary>
/// The main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly WindowStateService _windowState;

    /// <summary>
    /// Initializes the window, restores its placement and applies the theme.
    /// </summary>
    /// <param name="homePage">The main page.</param>
    /// <param name="themeService">The theme service.</param>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="windowState">The window state persistence service.</param>
    public MainWindow(
        HomePage homePage,
        ThemeService themeService,
        IAppSettingsService settingsService,
        WindowStateService windowState)
    {
        InitializeComponent();

        Title = "EasyEject";
        RootElement.Children.Add(homePage);

        // Mica backdrop for a modern Windows 11 look (ignored gracefully on older systems).
        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Mica backdrop is not available on this system.");
        }

        _windowState = windowState;

        var settings = settingsService.LoadAsync().GetAwaiter().GetResult();
        themeService.Attach(RootElement);
        themeService.Apply(settings.ThemeMode);

        _windowState.Restore(AppWindow, settings.Window);

        // Remember size and position when the window closes.
        AppWindow.Closing += (_, _) => _windowState.Save(AppWindow);

        // Enforce the minimum window size.
        WindowSizeGuard.SetMinSize(this, 650, 500);
    }
}
