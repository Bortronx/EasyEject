using EasyEject.Core.Contracts;
using EasyEject.Core.Services;
using EasyEject.Models;
using Microsoft.Extensions.Logging;

namespace EasyEject.Services;

/// <summary>
/// High-level device manager combining enumeration, single eject and eject-all.
/// </summary>
public sealed class DeviceManager : IDeviceManager
{
    private readonly IDeviceEnumerator _enumerator;
    private readonly EjectAllCoordinator _coordinator;
    private readonly ILogger<DeviceManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceManager"/> class.
    /// </summary>
    /// <param name="enumerator">The device enumerator.</param>
    /// <param name="ejector">The ejector.</param>
    /// <param name="logger">The logger.</param>
    public DeviceManager(IDeviceEnumerator enumerator, IEjector ejector, ILogger<DeviceManager> logger)
    {
        _enumerator = enumerator;
        _coordinator = new EjectAllCoordinator(ejector);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ExternalDevice>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        return _enumerator.EnumerateAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<EjectResult> EjectAsync(ExternalDevice device, CancellationToken cancellationToken = default)
    {
        _ = device ?? throw new ArgumentNullException(nameof(device));
        _logger.LogInformation("Ejecting single device '{FriendlyName}'.", device.FriendlyName);
        return _coordinator.EjectSingleAsync(device, cancellationToken);
    }

    /// <inheritdoc />
    public Task<EjectAllResult> EjectAllAsync(IProgress<EjectProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        return EjectAllInternalAsync(progress, cancellationToken);
    }

    private async Task<EjectAllResult> EjectAllInternalAsync(IProgress<EjectProgress>? progress, CancellationToken cancellationToken)
    {
        IReadOnlyList<ExternalDevice> devices = await RefreshAsync(cancellationToken).ConfigureAwait(false);
        var ejectable = devices.Where(d => d.CanEject).ToList();

        _logger.LogInformation("Eject-all requested for {Count} devices.", ejectable.Count);

        if (ejectable.Count == 0)
        {
            return new EjectAllResult(Array.Empty<EjectResult>(), 0, 0);
        }

        return await _coordinator.EjectAllAsync(ejectable, progress, cancellationToken).ConfigureAwait(false);
    }
}
