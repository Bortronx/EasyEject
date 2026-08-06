using EasyEject.Models;

namespace EasyEject.Core.Contracts;

/// <summary>
/// Requests user confirmation before destructive actions.
/// </summary>
public interface IUserConfirmationService
{
    /// <summary>
    /// Asks the user to confirm ejecting a single device.
    /// </summary>
    /// <param name="device">The device about to be ejected.</param>
    /// <returns><c>true</c> when the user confirmed.</returns>
    Task<bool> ConfirmEjectAsync(ExternalDevice device);

    /// <summary>
    /// Asks the user to confirm ejecting every device.
    /// </summary>
    /// <param name="deviceCount">The number of devices that will be ejected.</param>
    /// <returns><c>true</c> when the user confirmed.</returns>
    Task<bool> ConfirmEjectAllAsync(int deviceCount);
}
