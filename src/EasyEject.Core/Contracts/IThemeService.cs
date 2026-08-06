using EasyEject.Models;

namespace EasyEject.Core.Contracts;

/// <summary>
/// Manages the application theme (light / dark / system).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently configured theme mode.
    /// </summary>
    ThemeMode CurrentMode { get; }

    /// <summary>
    /// Sets the theme mode and applies it.
    /// </summary>
    /// <param name="mode">The requested mode.</param>
    void Apply(ThemeMode mode);

    /// <summary>
    /// Occurs when the system theme changes while in system-follow mode.
    /// </summary>
    event EventHandler<ThemeMode>? SystemThemeChanged;
}
