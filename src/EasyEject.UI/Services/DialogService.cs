using EasyEject.Core.Contracts;
using EasyEject.Core.Services;
using EasyEject.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EasyEject.UI.Services;

/// <summary>
/// Hosts all ContentDialogs: confirmations, eject-all progress and completion summaries.
/// </summary>
public sealed class DialogService : IUserConfirmationService
{
    private readonly ILogger<DialogService> _logger;
    private XamlRoot? _xamlRoot;

    private ContentDialog? _progressDialog;
    private ProgressBar? _progressBar;
    private TextBlock? _progressCurrent;
    private TextBlock? _progressCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DialogService(ILogger<DialogService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Binds the service to the window's XamlRoot.
    /// </summary>
    /// <param name="root">The XamlRoot of the main window.</param>
    public void Initialize(XamlRoot root) => _xamlRoot = root;

    /// <inheritdoc />
    public async Task<bool> ConfirmEjectAsync(ExternalDevice device)
    {
        string name = device.FriendlyName ?? device.Model ?? device.DeviceId;

        var dialog = new ContentDialog
        {
            XamlRoot = _xamlRoot,
            Title = "Eject device",
            Content = $"This will safely eject \"{name}\". Close any open files first to avoid data loss.",
            PrimaryButtonText = "Eject",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        return await ShowAsync(dialog) == ContentDialogResult.Primary;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmEjectAllAsync(int deviceCount)
    {
        string detail = deviceCount == 1
            ? "1 removable storage device will be ejected."
            : $"{deviceCount} removable storage devices will be ejected, one at a time.";

        var dialog = new ContentDialog
        {
            XamlRoot = _xamlRoot,
            Title = "Eject all devices",
            Content = $"This will safely eject every removable storage device connected to this computer.\n\n{detail}",
            PrimaryButtonText = "Eject",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        return await ShowAsync(dialog) == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Shows the eject-all progress dialog and completes when it is closed.
    /// </summary>
    /// <param name="total">The total number of devices to eject.</param>
    public async Task ShowEjectProgressAsync(int total)
    {
        _progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = Math.Max(total, 1),
            Value = 0,
            Width = 360,
            Margin = new Thickness(0, 8, 0, 0),
        };

        _progressCurrent = new TextBlock
        {
            Text = "Preparing...",
            TextWrapping = TextWrapping.Wrap,
        };

        _progressCount = new TextBlock
        {
            Text = $"0 / {total}",
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"],
        };

        var content = new StackPanel
        {
            Spacing = 4,
            Width = 400,
            Children = { _progressCurrent, _progressCount, _progressBar },
        };

        _progressDialog = new ContentDialog
        {
            XamlRoot = _xamlRoot,
            Title = "Ejecting devices...",
            Content = content,
            PrimaryButtonText = null,
            CloseButtonText = null,
        };

        await ShowAsync(_progressDialog);
    }

    /// <summary>
    /// Updates the eject-all progress dialog.
    /// </summary>
    /// <param name="currentName">The device currently being ejected.</param>
    /// <param name="completed">The number of completed devices.</param>
    /// <param name="total">The total number of devices.</param>
    public void UpdateEjectProgress(string? currentName, int completed, int total)
    {
        if (_progressCurrent is not null && _progressBar is not null)
        {
            _progressCurrent.Text = currentName ?? "Ejecting...";
            if (_progressCount is not null)
            {
                _progressCount.Text = $"{completed} / {total}";
            }

            _progressBar.Maximum = Math.Max(total, 1);
            _progressBar.Value = completed;
        }
    }

    /// <summary>
    /// Closes the eject-all progress dialog.
    /// </summary>
    public void CloseEjectProgress()
    {
        try
        {
            _progressDialog?.Hide();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to hide the progress dialog.");
        }

        _progressDialog = null;
        _progressBar = null;
        _progressCurrent = null;
        _progressCount = null;
    }

    /// <summary>
    /// Shows the completion summary of an eject-all operation.
    /// </summary>
    /// <param name="result">The aggregated result.</param>
    public async Task ShowEjectAllResultAsync(EjectAllResult result)
    {
        var rows = new StackPanel { Spacing = 10 };

        foreach (EjectResult item in result.Results)
        {
            bool success = item.IsSuccess;

            var glyph = new FontIcon
            {
                Glyph = success ? "\uE930" : "\uE7BA",
                FontSize = 16,
                Foreground = success
                    ? (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SystemFillColorSuccessBrush"]
                    : (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SystemFillColorCriticalBrush"],
            };

            var statusText = new TextBlock
            {
                Text = EjectMessages.TitleFor(item.Status),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var detail = new TextBlock
            {
                Text = item.Status switch
                {
                    EjectStatus.Success => "Safely ejected.",
                    EjectStatus.AlreadyRemoved => "Already removed.",
                    _ => item.Message ?? string.Empty,
                },
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var header = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
            };

            header.Children.Add(glyph);
            Grid.SetColumn(statusText, 1);
            Grid.SetColumn(detail, 2);
            header.Children.Add(statusText);
            header.Children.Add(detail);

            rows.Children.Add(header);
        }

        var dialog = new ContentDialog
        {
            XamlRoot = _xamlRoot,
            Title = "Eject complete",
            Content = new ScrollViewer
            {
                MaxHeight = 320,
                Content = rows,
            },
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
        };

        await ShowAsync(dialog);
    }

    private async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        try
        {
            // Lazily resolve XamlRoot if it was not yet available at
            // Initialize() time (e.g. during the MainWindow constructor).
            if (dialog.XamlRoot is null && _xamlRoot is not null)
            {
                dialog.XamlRoot = _xamlRoot;
            }

            return await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dialog could not be shown.");
            return ContentDialogResult.None;
        }
    }
}
