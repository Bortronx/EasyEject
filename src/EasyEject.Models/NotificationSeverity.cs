namespace EasyEject.Models;

/// <summary>
/// Severity of a user-facing notification message.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>An informative message.</summary>
    Information = 0,

    /// <summary>A success confirmation message.</summary>
    Success = 1,

    /// <summary>A warning message.</summary>
    Warning = 2,

    /// <summary>An error message.</summary>
    Error = 3,
}