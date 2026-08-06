namespace EasyEject.Models;

/// <summary>
/// Persisted application settings.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the requested application theme.
    /// </summary>
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    /// <summary>
    /// Gets or sets the saved main window placement.
    /// </summary>
    public WindowPlacement? Window { get; set; }
}

/// <summary>
/// A persisted main window placement.
/// </summary>
public sealed class WindowPlacement
{
    /// <summary>Gets or sets the left edge of the window, in screen pixels.</summary>
    public int X { get; set; }

    /// <summary>Gets or sets the top edge of the window, in screen pixels.</summary>
    public int Y { get; set; }

    /// <summary>Gets or sets the window width, in screen pixels.</summary>
    public int Width { get; set; } = 900;

    /// <summary>Gets or sets the window height, in screen pixels.</summary>
    public int Height { get; set; } = 640;

    /// <summary>Gets or sets whether the window was maximized.</summary>
    public bool IsMaximized { get; set; }
}
