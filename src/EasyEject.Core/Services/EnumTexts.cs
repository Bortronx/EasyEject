using EasyEject.Models;

namespace EasyEject.Core.Services;

/// <summary>
/// Maps model enums to user-facing display strings.
/// </summary>
public static class EnumTexts
{
    /// <summary>
    /// Returns a friendly label for a <see cref="BusType"/>.
    /// </summary>
    public static string BusType(BusType busType)
    {
        return busType switch
        {
            Models.BusType.Usb => "USB",
            Models.BusType.Sata => "SATA",
            Models.BusType.Nvme => "NVMe",
            Models.BusType.Scsi => "SCSI",
            Models.BusType.Ata => "ATA",
            Models.BusType.Sd => "SD",
            Models.BusType.Mmc => "MMC",
            Models.BusType.IEEE1394 => "FireWire",
            Models.BusType.IScsi => "iSCSI",
            Models.BusType.Sas => "SAS",
            Models.BusType.Virtual => "Virtual",
            _ => "Unknown",
        };
    }

    /// <summary>
    /// Returns a friendly label for a <see cref="DeviceType"/>.
    /// </summary>
    public static string DeviceType(DeviceType deviceType)
    {
        return deviceType switch
        {
            Models.DeviceType.UsbFlashDrive => "USB Flash Drive",
            Models.DeviceType.UsbHardDrive => "External HDD",
            Models.DeviceType.UsbSolidStateDrive => "External SSD",
            Models.DeviceType.UsbNvme => "USB NVMe",
            Models.DeviceType.UsbSataAdapter => "USB SATA",
            Models.DeviceType.SdCard => "SD Card",
            Models.DeviceType.CardReader => "Card Reader",
            _ => "Unknown Device",
        };
    }

    /// <summary>
    /// Returns the drive letter display text, for example "D:" or "—".
    /// </summary>
    public static string DriveLetters(IReadOnlyList<string> letters)
    {
        return letters.Count > 0 ? string.Join(", ", letters) : "—";
    }
}
