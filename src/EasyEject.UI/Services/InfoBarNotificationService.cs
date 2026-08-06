using EasyEject.Core.Contracts;
using EasyEject.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace EasyEject.UI.Services;

/// <summary>
/// Shows modern InfoBar-style notifications inside the main page.
/// </summary>
public sealed class InfoBarNotificationService : INotificationService
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(6);

    private InfoBar? _bar;
    private DispatcherQueueTimer? _hideTimer;

    /// <inheritdoc />
    public event EventHandler<NotificationMessage>? NotificationRaised;

    /// <inheritdoc />
    public event EventHandler? NotificationCleared;

    /// <summary>
    /// Attaches the service to the InfoBar control of the main page.
    /// </summary>
    /// <param name="bar">The InfoBar control.</param>
    public void Attach(InfoBar bar)
    {
        _bar = bar;
        _hideTimer = bar.DispatcherQueue.CreateTimer();
        _hideTimer.Interval = DisplayDuration;
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            _bar.IsOpen = false;
        };
    }

    /// <inheritdoc />
    public void Show(NotificationMessage notification)
    {
        if (_bar is not null)
        {
            _bar.Severity = notification.Severity switch
            {
                NotificationSeverity.Success => InfoBarSeverity.Success,
                NotificationSeverity.Warning => InfoBarSeverity.Warning,
                NotificationSeverity.Error => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational,
            };
            _bar.Title = notification.Title;
            _bar.Message = notification.Message;
            _bar.IsOpen = true;

            _hideTimer?.Start();
        }

        NotificationRaised?.Invoke(this, notification);
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_bar is not null)
        {
            _hideTimer?.Stop();
            _bar.IsOpen = false;
        }

        NotificationCleared?.Invoke(this, EventArgs.Empty);
    }
}
