using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyEject.Core.Contracts;
using EasyEject.Core.Services;
using EasyEject.Models;
using EasyEject.UI.Services;
using Microsoft.Extensions.Logging;

namespace EasyEject.UI.ViewModels;

/// <summary>
/// Main page view model: device list, scan, eject all and eject selected.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IDeviceManager _deviceManager;
    private readonly IUserConfirmationService _confirmations;
    private readonly INotificationService _notifications;
    private readonly DialogService _dialogs;
    private readonly IThemeService _themeService;
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<HomeViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    public HomeViewModel(
        IDeviceManager deviceManager,
        IUserConfirmationService confirmations,
        INotificationService notifications,
        DialogService dialogs,
        IThemeService themeService,
        IAppSettingsService settingsService,
        ILogger<HomeViewModel> logger)
    {
        _deviceManager = deviceManager;
        _confirmations = confirmations;
        _notifications = notifications;
        _dialogs = dialogs;
        _themeService = themeService;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of detected devices.
    /// </summary>
    public ObservableCollection<DeviceItemViewModel> Devices { get; } = new();

    /// <summary>
    /// Gets the theme options exposed to the theme picker.
    /// </summary>
    public IReadOnlyList<ThemeOption> ThemeOptions { get; } =
    [
        new ThemeOption("System", ThemeMode.System),
        new ThemeOption("Light", ThemeMode.Light),
        new ThemeOption("Dark", ThemeMode.Dark),
    ];

    /// <summary>
    /// Gets or sets the currently selected device.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EjectSelectedCommand))]
    public partial DeviceItemViewModel? SelectedDevice { get; set; }

    /// <summary>
    /// Gets or sets whether a scan or ejection is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EjectSelectedCommand))]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Gets or sets the status bar text.
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready.";

    /// <summary>
    /// Gets or sets the number of detected devices.
    /// </summary>
    [ObservableProperty]
    public partial int DeviceCount { get; set; }

    /// <summary>
    /// Gets or sets the theme picker selection.
    /// </summary>
    [ObservableProperty]
    public partial ThemeOption? SelectedThemeOption { get; set; }

    /// <summary>
    /// Gets a value indicating whether any devices are shown.
    /// </summary>
    public bool HasDevices => Devices.Count > 0;

    /// <summary>
    /// Gets the status bar device count text.
    /// </summary>
    public string DeviceCountText => DeviceCount == 0 ? "No devices" : $"{DeviceCount} device{(DeviceCount == 1 ? string.Empty : "s")}";

    partial void OnSelectedThemeOptionChanged(ThemeOption? value)
    {
        if (value is null)
        {
            return;
        }

        _themeService.Apply(value.Mode);
        PersistThemeAsync(value.Mode);
    }

    private async void PersistThemeAsync(ThemeMode mode)
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            settings.ThemeMode = mode;
            await _settingsService.SaveAsync(settings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist theme selection.");
        }
    }

    /// <summary>
    /// Called once when the page loads: restores the theme and runs the first scan.
    /// </summary>
    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync();
        SelectedThemeOption = ThemeOptions.FirstOrDefault(o => o.Mode == settings.ThemeMode) ?? ThemeOptions[0];

        await RefreshCoreAsync();
    }

    /// <summary>
    /// Manually refreshes the device list.
    /// </summary>
    [RelayCommand]
    internal async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        StatusText = "Scanning devices...";
        await ScanDevicesAsync();
        StatusText = StatusForCount(Devices.Count);
    }

    private async Task RefreshCoreAsync(bool quiet = false)
    {
        if (IsBusy)
        {
            return;
        }

        if (!quiet)
        {
            StatusText = "Scanning devices...";
        }

        await ScanDevicesAsync();

        if (!quiet)
        {
            StatusText = StatusForCount(Devices.Count);
        }
    }

    private async Task ScanDevicesAsync()
    {
        IsBusy = true;
        try
        {
            IReadOnlyList<ExternalDevice> fresh = await _deviceManager.RefreshAsync();
            MergeDevices(fresh);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device scan failed.");
            _notifications.Show(new NotificationMessage(
                "Scan failed",
                "The device list could not be refreshed. Check the logs for details.",
                NotificationSeverity.Error));
        }
        finally
        {
            IsBusy = false;
            DeviceCount = Devices.Count;
        }
    }

    private static string StatusForCount(int count)
    {
        return count == 0
            ? "Ready. No external devices found."
            : $"Ready. {count} external device{(count == 1 ? string.Empty : "s")} detected.";
    }

    private void MergeDevices(IReadOnlyList<ExternalDevice> fresh)
    {
        string? selectedId = SelectedDevice?.Device.DeviceId;
        var existing = Devices.ToDictionary(d => d.Device.DeviceId, StringComparer.OrdinalIgnoreCase);

        Devices.Clear();

        foreach (ExternalDevice device in fresh)
        {
            if (existing.TryGetValue(device.DeviceId, out DeviceItemViewModel? old))
            {
                old.Update(device);
                Devices.Add(old);
            }
            else
            {
                Devices.Add(new DeviceItemViewModel(device));
            }
        }

        SelectedDevice = selectedId is null
            ? null
            : Devices.FirstOrDefault(d => string.Equals(d.Device.DeviceId, selectedId, StringComparison.OrdinalIgnoreCase));

        OnPropertyChanged(nameof(HasDevices));
    }

    /// <summary>
    /// Selects a device from the list (radio or list selection).
    /// </summary>
    /// <param name="item">The device to select.</param>
    public void Select(DeviceItemViewModel item)
    {
        if (SelectedDevice == item)
        {
            return;
        }

        if (SelectedDevice is not null)
        {
            SelectedDevice.IsSelected = false;
        }

        item.IsSelected = true;
        SelectedDevice = item;
    }

    /// <summary>
    /// Ejects the selected device after confirmation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEjectSelected))]
    private async Task EjectSelectedAsync()
    {
        DeviceItemViewModel? selected = SelectedDevice;
        if (selected is null || !selected.CanEject || IsBusy)
        {
            return;
        }

        ExternalDevice device = selected.Device;

        bool confirmed = await _confirmations.ConfirmEjectAsync(device);
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Ejecting...";
        try
        {
            EjectResult result = await _deviceManager.EjectAsync(device);

            if (result.IsSuccess)
            {
                _notifications.Show(new NotificationMessage(
                    "Ejected successfully",
                    $"{result.FriendlyName ?? "Device"} safely ejected.",
                    NotificationSeverity.Success));
            }
            else
            {
                _notifications.Show(new NotificationMessage(
                    EjectMessages.TitleFor(result.Status),
                    EjectMessages.ExplanationFor(result.Status, result.Message),
                    NotificationSeverity.Warning));
            }

            await ScanDevicesAsync();
            StatusText = StatusForCount(Devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Single eject failed.");
            _notifications.Show(new NotificationMessage(
                "Eject failed",
                "An unexpected error occurred while ejecting the device.",
                NotificationSeverity.Error));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanEjectSelected() => SelectedDevice is { CanEject: true } && !IsBusy;

    /// <summary>
    /// Ejects every removable device after confirmation.
    /// </summary>
    [RelayCommand]
    private async Task EjectAllAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var ejectable = Devices.Where(d => d.CanEject).ToList();
        if (ejectable.Count == 0)
        {
            _notifications.Show(new NotificationMessage(
                "Nothing to eject",
                "No removable storage devices are connected.",
                NotificationSeverity.Information));
            return;
        }

        bool confirmed = await _confirmations.ConfirmEjectAllAsync(ejectable.Count);
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Ejecting...";
        try
        {
            var progress = new Progress<EjectProgress>(p =>
                _dialogs.UpdateEjectProgress(p.CurrentName, p.Completed, p.Total));

            Task dialogTask = _dialogs.ShowEjectProgressAsync(ejectable.Count);

            EjectAllResult result;
            try
            {
                result = await _deviceManager.EjectAllAsync(progress);
            }
            finally
            {
                _dialogs.CloseEjectProgress();
            }

            try
            {
                await dialogTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Progress dialog closed unexpectedly.");
            }

            await ScanDevicesAsync();

            StatusText = EjectMessages.SummaryFor(result);
            _logger.LogInformation(
                "Eject-all finished: {Succeeded} succeeded, {Failed} failed.",
                result.SucceededCount, result.FailedCount);

            _notifications.Show(new NotificationMessage(
                result.FailedCount == 0 ? "All devices ejected" : "Some devices could not be ejected",
                EjectMessages.SummaryFor(result),
                result.FailedCount == 0 ? NotificationSeverity.Success : NotificationSeverity.Warning));

            await _dialogs.ShowEjectAllResultAsync(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Eject-all was cancelled.");
            StatusText = StatusForCount(Devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eject-all failed.");
            _notifications.Show(new NotificationMessage(
                "Eject failed",
                "An unexpected error occurred while ejecting the devices.",
                NotificationSeverity.Error));
            StatusText = StatusForCount(Devices.Count);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
