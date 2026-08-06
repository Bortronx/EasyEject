namespace EasyEject.Models;

/// <summary>
/// Represents the storage bus a device is attached to.
/// Values mirror the Win32 <c>STORAGE_BUS_TYPE</c> enumeration.
/// </summary>
public enum BusType
{
    /// <summary>The bus type could not be determined.</summary>
    Unknown = 0,

    /// <summary>SCSI bus.</summary>
    Scsi = 1,

    /// <summary>ATA bus.</summary>
    Ata = 3,

    /// <summary>IEEE 1394 (FireWire) bus.</summary>
    IEEE1394 = 4,

    /// <summary>USB bus.</summary>
    Usb = 7,

    /// <summary>iSCSI bus.</summary>
    IScsi = 9,

    /// <summary>SAS bus.</summary>
    Sas = 10,

    /// <summary>SATA bus.</summary>
    Sata = 11,

    /// <summary>SD/MMC bus (memory cards).</summary>
    Sd = 12,

    /// <summary>MMC bus.</summary>
    Mmc = 13,

    /// <summary>Virtual bus (RAM disks, file-backed devices).</summary>
    Virtual = 14,

    /// <summary>File-backed virtual bus.</summary>
    FileBackedVirtual = 15,

    /// <summary>Storage Spaces bus.</summary>
    Spaces = 16,

    /// <summary>NVMe (PCI Express) bus.</summary>
    Nvme = 17,
}
