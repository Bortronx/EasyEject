namespace EasyEject.Models;

/// <summary>
/// Specifies the application theme.
/// </summary>
public enum ThemeMode
{
    /// <summary>Follow the current Windows theme.</summary>
    System = 0,

    /// <summary>Always use light theme.</summary>
    Light = 1,

    /// <summary>Always use dark theme.</summary>
    Dark = 2,
}