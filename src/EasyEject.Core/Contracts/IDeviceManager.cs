using EasyEject.Models;

namespace EasyEject.Core.Contracts;

/// <summary>
/// Progress information reported while ejecting multiple devices.
/// </summary>
/// <param name="Completed">The number of devices already processed.</param>
/// <param name="Total">The total number of devices being processed.</param>
/// <param name="CurrentName">The friendly name of the device being processed.</param>
/// <param name="CurrentStatus">The status of the most recently completed device.</param>
public sealed record EjectProgress(int Completed, int Total, string? CurrentName, EjectStatus? CurrentStatus);

/// <summary>
/// High-level facade for scanning devices and ejecting them.
/// </summary>
public interface IDeviceManager
{
    /// <summary>
    /// Scans the computer and returns the current external devices.
    /// </summary>
    Task<IReadOnlyList<ExternalDevice>> RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely ejects a single device.
    /// </summary>
    Task<EjectResult> EjectAsync(ExternalDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely ejects every removable device, one at a time.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated result.</returns>
    Task<EjectAllResult> EjectAllAsync(IProgress<EjectProgress>? progress = null, CancellationToken cancellationToken = default);
}
