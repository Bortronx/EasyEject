using EasyEject.Core.Contracts;
using EasyEject.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace EasyEject.UI.Services;

/// <summary>
/// Manages light/dark theme, following the Windows system theme by default
/// and supporting a manual override. Settings are persisted by the caller.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly UISettings _uiSettings = new();
    private FrameworkElement? _rootElement;
    private DispatcherQueue? _dispatcherQueue;
    private ThemeMode _currentMode = ThemeMode.System;

    /// <summary>
    /// Occurs when the system theme changes while following the system theme.
    /// </summary>
    public event EventHandler<ThemeMode>? SystemThemeChanged;

    /// <summary>
    /// Gets the currently configured theme mode.
    /// </summary>
    public ThemeMode CurrentMode => _currentMode;

    /// <summary>
    /// Attaches the service to the root element of a window.
    /// </summary>
    /// <param name="rootElement">The window's root framework element.</param>
    public void Attach(FrameworkElement rootElement)
    {
        _rootElement = rootElement;
        _dispatcherQueue = rootElement.DispatcherQueue;
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
    }

    /// <summary>
    /// Applies the requested theme mode to the attached root element.
    /// </summary>
    /// <param name="mode">The requested theme mode.</param>
    public void Apply(ThemeMode mode)
    {
        _currentMode = mode;

        if (_rootElement is null)
        {
            return;
        }

        _rootElement.RequestedTheme = mode switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    /// <summary>
    /// Determines whether the current Windows system theme is dark.
    /// </summary>
    private bool IsSystemDark()
    {
        Windows.UI.Color background = _uiSettings.GetColorValue(UIColorType.Background);
        double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
        return luminance < 0.5;
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        if (_currentMode != ThemeMode.System || _dispatcherQueue is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            ThemeMode effective = IsSystemDark() ? ThemeMode.Dark : ThemeMode.Light;
            Apply(effective);
            SystemThemeChanged?.Invoke(this, effective);
        });
    }
}
