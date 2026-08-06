using EasyEject.Models;

namespace EasyEject.Core.Contracts;

/// <summary>
/// Safely ejects a single storage device.
/// </summary>
public interface IEjector
{
    /// <summary>
    /// Safely ejects the specified device.
    /// </summary>
    /// <param name="device">The device to eject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the eject attempt.</returns>
    Task<EjectResult> EjectAsync(ExternalDevice device, CancellationToken cancellationToken = default);
}
