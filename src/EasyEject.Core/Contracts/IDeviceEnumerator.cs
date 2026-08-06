using EasyEject.Models;

namespace EasyEject.Core.Contracts;

/// <summary>
/// Enumerates the external storage devices currently connected to this computer.
/// </summary>
public interface IDeviceEnumerator
{
    /// <summary>
    /// Enumerates removable storage devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of connected external devices.</returns>
    Task<IReadOnlyList<ExternalDevice>> EnumerateAsync(CancellationToken cancellationToken = default);
}
