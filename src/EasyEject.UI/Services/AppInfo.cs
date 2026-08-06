using System.Reflection;

namespace EasyEject.UI.Services;

/// <summary>
/// Application metadata.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// Gets the application version (1.0.0).
    /// </summary>
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
}
