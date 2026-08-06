using System;
using EasyEject.Core.Contracts;
using EasyEject.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace EasyEject.UI.Services;

/// <summary>
/// Restores and persists the main window size, position and maximize state.
/// </summary>
public sealed class WindowStateService
{
    private const int DefaultWidth = 960;
    private const int DefaultHeight = 680;
    private const int Margin = 24;

    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<WindowStateService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowStateService"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="logger">The logger.</param>
    public WindowStateService(IAppSettingsService settingsService, ILogger<WindowStateService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Applies a previously saved placement to the window, clamped to the visible desktop.
    /// </summary>
    /// <param name="window">The AppWindow to position.</param>
    /// <param name="placement">The saved placement, or <c>null</c> for the default.</param>
    public void Restore(AppWindow window, WindowPlacement? placement)
    {
        try
        {
            DisplayArea display = DisplayArea.GetFromWindowId(window.Id, DisplayAreaFallback.Primary);
            RectInt32 work = display.WorkArea;

            int width;
            int height;
            int x;
            int y;

            if (placement is not null && placement.Width >= 650 && placement.Height >= 500)
            {
                width = Math.Min(placement.Width, Math.Max(work.Width - Margin, 650));
                height = Math.Min(placement.Height, Math.Max(work.Height - Margin, 500));
                x = placement.X;
                y = placement.Y;

                // Keep the window on screen (handles monitors being unplugged).
                if (x + width - 50 < work.X)
                {
                    x = work.X;
                }

                if (y + height - 50 < work.Y)
                {
                    y = work.Y;
                }

                if (x > work.X + work.Width - Margin)
                {
                    x = work.X + work.Width - width;
                }

                if (y > work.Y + work.Height - Margin)
                {
                    y = work.Y + work.Height - height;
                }
            }
            else
            {
                width = Math.Min(DefaultWidth, Math.Max(work.Width - Margin, 650));
                height = Math.Min(DefaultHeight, Math.Max(work.Height - Margin, 500));
                x = work.X + (work.Width - width) / 2;
                y = work.Y + (work.Height - height) / 2;
            }

            window.MoveAndResize(new RectInt32(x, y, width, height));

            if (placement is { IsMaximized: true } && window.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not restore window placement; using defaults.");
        }
    }

    /// <summary>
    /// Persists the window placement and size.
    /// </summary>
    /// <param name="window">The AppWindow to save.</param>
    public void Save(AppWindow window)
    {
        try
        {
            var settings = new AppSettings
            {
                ThemeMode = ThemeMode.System,
                Window = new WindowPlacement
                {
                    X = window.Position.X,
                    Y = window.Position.Y,
                    Width = window.Size.Width,
                    Height = window.Size.Height,
                    IsMaximized = window.Presenter is OverlappedPresenter presenter
                        && presenter.State == OverlappedPresenterState.Maximized,
                },
            };

            var current = _settingsService.LoadAsync().GetAwaiter().GetResult();
            current.Window = settings.Window;
            _settingsService.SaveAsync(current).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save window placement.");
        }
    }
}
