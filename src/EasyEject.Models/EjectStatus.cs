namespace EasyEject.Models;

/// <summary>
/// Describes the outcome of a safe-eject request.
/// </summary>
public enum EjectStatus
{
    /// <summary>The device was ejected successfully.</summary>
    Success = 0,

    /// <summary>The device had already been removed before the request completed.</summary>
    AlreadyRemoved = 1,

    /// <summary>The device is currently busy and cannot be locked.</summary>
    DeviceBusy = 2,

    /// <summary>Windows reported that the device is in use.</summary>
    DeviceInUse = 3,

    /// <summary>The caller does not have permission to eject the device.</summary>
    AccessDenied = 4,

    /// <summary>The device cannot be ejected from this computer.</summary>
    NotRemovable = 5,

    /// <summary>The eject request was vetoed by the system with a message.</summary>
    Vetoed = 6,

    /// <summary>An unexpected failure occurred.</summary>
    Failed = 7,
}