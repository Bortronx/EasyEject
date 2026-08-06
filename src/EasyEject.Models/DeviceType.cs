namespace EasyEject.Models;

/// <summary>
/// Classifies the physical kind of an external storage device.
/// </summary>
public enum DeviceType
{
    /// <summary>Device type could not be determined.</summary>
    Unknown = 0,

    /// <summary>A USB flash drive (thumb drive).</summary>
    UsbFlashDrive = 1,

    /// <summary>An external hard disk drive attached over USB.</summary>
    UsbHardDrive = 2,

    /// <summary>An external solid state drive attached over USB.</summary>
    UsbSolidStateDrive = 3,

    /// <summary>A USB-attached NVMe drive.</summary>
    UsbNvme = 4,

    /// <summary>A USB to SATA bridge/adapter.</summary>
    UsbSataAdapter = 5,

    /// <summary>An SD memory card.</summary>
    SdCard = 6,

    /// <summary>A memory card reader.</summary>
    CardReader = 7,
}