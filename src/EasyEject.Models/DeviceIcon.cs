namespace EasyEject.Models;

/// <summary>
/// Identifies the glyph used to render a device in the user interface.
/// </summary>
public enum DeviceIcon
{
    /// <summary>A generic USB device icon.</summary>
    Usb = 0,

    /// <summary>A solid state drive icon.</summary>
    SolidStateDrive = 1,

    /// <summary>A hard disk drive icon.</summary>
    HardDrive = 2,

    /// <summary>An SD / MMC memory card icon.</summary>
    MemoryCard = 3,

    /// <summary>A fallback icon used when nothing else matches.</summary>
    Generic = 4,
}