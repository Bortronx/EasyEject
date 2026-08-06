using EasyEject.Models;

namespace EasyEject.Core.Services;

/// <summary>
/// Classifies raw device properties into user-facing device types and icons.
/// </summary>
public static class DeviceClassifier
{
    /// <summary>
    /// Determines the device type from bus and product information.
    /// </summary>
    /// <param name="busType">The storage bus type.</param>
    /// <param name="productName">The product/model name, when available.</param>
    /// <returns>The classified device type.</returns>
    public static DeviceType Classify(BusType busType, string? productName)
    {
        string? name = productName?.ToUpperInvariant();
        bool hasName = !string.IsNullOrWhiteSpace(name);

        bool Contains(string token) => name is not null && name.Contains(token, StringComparison.Ordinal);

        if (busType == BusType.Sd || busType == BusType.Mmc || Contains("SDXC") || Contains("SDHC") || Contains("MICROSD") || Contains("SD CARD"))
        {
            return DeviceType.SdCard;
        }

        if (hasName && (Contains("CARD READER") || Contains("CARDREADER") || Contains("MULTI READER") || Contains("CARD-RW") || Contains("CRW")))
        {
            return DeviceType.CardReader;
        }

        if (Contains("NVME"))
        {
            return DeviceType.UsbNvme;
        }

        if (Contains("SSD") || Contains("PSSD") || Contains("SANDISK EXTREME PORTABLE") || Contains("T7"))
        {
            return DeviceType.UsbSolidStateDrive;
        }

        if (Contains("SATA") || Contains("S-ATA"))
        {
            return DeviceType.UsbSataAdapter;
        }

        if (busType == BusType.Usb)
        {
            return DeviceType.UsbFlashDrive;
        }

        if (busType is BusType.Sata or BusType.Ata or BusType.Scsi or BusType.Nvme)
        {
            return Contains("SSD") ? DeviceType.UsbSolidStateDrive : DeviceType.UsbHardDrive;
        }

        return DeviceType.Unknown;
    }

    /// <summary>
    /// Determines the icon used to represent a classified device.
    /// </summary>
    /// <param name="deviceType">The classified device type.</param>
    /// <returns>The matching icon kind.</returns>
    public static DeviceIcon IconFor(DeviceType deviceType)
    {
        return deviceType switch
        {
            DeviceType.SdCard or DeviceType.CardReader => DeviceIcon.MemoryCard,
            DeviceType.UsbSolidStateDrive or DeviceType.UsbNvme or DeviceType.UsbSataAdapter => DeviceIcon.SolidStateDrive,
            DeviceType.UsbHardDrive => DeviceIcon.HardDrive,
            DeviceType.UsbFlashDrive => DeviceIcon.Usb,
            _ => DeviceIcon.Generic,
        };
    }

    /// <summary>
    /// Determines whether a device can be safely ejected.
    /// </summary>
    /// <param name="isRemovable">Whether Windows marks the device as removable.</param>
    /// <param name="busType">The storage bus type.</param>
    /// <returns><c>true</c> when the device can be ejected.</returns>
    public static bool CanEject(bool isRemovable, BusType busType)
    {
        // Windows does not always flag USB disks as "removable"; always treat
        // physically removable transports as ejectable.
        return isRemovable || busType is BusType.Usb or BusType.Sd or BusType.Mmc or BusType.IEEE1394;
    }
}
