using EasyEject.Models;

namespace EasyEject.Core.Contracts;

/// <summary>
/// A user-facing notification message.
/// </summary>
/// <param name="Title">The notification title.</param>
/// <param name="Message">The notification body.</param>
/// <param name="Severity">The notification severity.</param>
public sealed record NotificationMessage(string Title, string? Message = null, NotificationSeverity Severity = NotificationSeverity.Information);

/// <summary>
/// Displays modern in-app notifications (InfoBar style).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a notification message.
    /// </summary>
    /// <param name="notification">The message to show.</param>
    void Show(NotificationMessage notification);

    /// <summary>
    /// Clears the currently displayed notification, if any.
    /// </summary>
    void Clear();

    /// <summary>
    /// Occurs when a notification should be displayed.
    /// </summary>
    event EventHandler<NotificationMessage>? NotificationRaised;

    /// <summary>
    /// Occurs when the current notification should be dismissed.
    /// </summary>
    event EventHandler? NotificationCleared;
}
