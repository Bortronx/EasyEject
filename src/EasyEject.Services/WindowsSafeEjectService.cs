using EasyEject.Core.Contracts;
using EasyEject.Core.Services;
using EasyEject.Models;
using EasyEject.Win32;
using Microsoft.Extensions.Logging;

namespace EasyEject.Services;

/// <summary>
/// Safely ejects devices using the documented Win32 eject APIs.
/// </summary>
public sealed class WindowsSafeEjectService : IEjector
{
    private readonly ILogger<WindowsSafeEjectService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsSafeEjectService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WindowsSafeEjectService(ILogger<WindowsSafeEjectService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<EjectResult> EjectAsync(ExternalDevice device, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Eject(device, cancellationToken), cancellationToken);
    }

    private EjectResult Eject(ExternalDevice device, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Eject request for '{FriendlyName}' ({DeviceId}).", device.FriendlyName, device.DeviceId);

        var ownVolumes = VolumeEnumerator.EnumerateVolumes()
            .Where(v => v.DiskNumbers.Contains(device.DiskNumber))
            .ToList();

        var info = new DeviceInfo(
            device.DeviceId,
            device.FriendlyName,
            device.Vendor,
            device.DevicePath ?? string.Empty,
            device.DiskNumber,
            device.BusType,
            device.IsRemovable ? 1u : 0u,
            device.CapacityBytes,
            device.ConnectedSince,
            null);

        EjectOutcome outcome;
        try
        {
            outcome = DeviceEjector.Eject(info, ownVolumes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error while ejecting '{FriendlyName}'.", device.FriendlyName);
            return new EjectResult(device.DeviceId, device.FriendlyName, EjectStatus.Failed, ex.Message);
        }

        string? message = outcome.Status switch
        {
            EjectStatus.Success => "safely ejected.",
            EjectStatus.AlreadyRemoved => "device is no longer connected.",
            _ => EjectMessages.ExplanationFor(outcome.Status, outcome.Detail),
        };

        _logger.LogInformation(
            "Eject result for '{FriendlyName}': {Status} ({Detail}).",
            device.FriendlyName, outcome.Status, outcome.Detail ?? message);

        return new EjectResult(device.DeviceId, device.FriendlyName, outcome.Status, message);
    }
}