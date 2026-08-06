namespace EasyEject.Models;

/// <summary>
/// The outcome of attempting to eject a single device.
/// </summary>
/// <param name="DeviceId">The device instance ID the request targeted.</param>
/// <param name="FriendlyName">The friendly name of the device.</param>
/// <param name="Status">The result status.</param>
/// <param name="Message">A user-friendly explanation of the result.</param>
public sealed record EjectResult(
    string DeviceId,
    string? FriendlyName,
    EjectStatus Status,
    string? Message = null)
{
    /// <summary>
    /// Gets a value indicating whether the eject request completed successfully.
    /// </summary>
    public bool IsSuccess => Status is EjectStatus.Success or EjectStatus.AlreadyRemoved;
}

/// <summary>
/// The aggregated outcome of an "eject all" operation.
/// </summary>
/// <param name="Results">The per-device results.</param>
/// <param name="SucceededCount">The number of devices that were ejected.</param>
/// <param name="FailedCount">The number of devices that could not be ejected.</param>
public sealed record EjectAllResult(
    IReadOnlyList<EjectResult> Results,
    int SucceededCount,
    int FailedCount);
