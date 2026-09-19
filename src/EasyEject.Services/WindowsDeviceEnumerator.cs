using EasyEject.Core.Contracts;
using EasyEject.Core.Services;
using EasyEject.Models;
using EasyEject.Win32;
using Microsoft.Extensions.Logging;

namespace EasyEject.Services;

/// <summary>
/// Enumerates external storage devices using the Win32 device tree.
/// </summary>
public sealed class WindowsDeviceEnumerator : IDeviceEnumerator
{
    private readonly ILogger<WindowsDeviceEnumerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsDeviceEnumerator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public WindowsDeviceEnumerator(ILogger<WindowsDeviceEnumerator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ExternalDevice>> EnumerateAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Enumerate(), cancellationToken);
    }

    private IReadOnlyList<ExternalDevice> Enumerate()
    {
        _logger.LogDebug("Starting device enumeration.");

        IReadOnlyList<DeviceInfo> disks = DiskDeviceEnumerator.EnumerateDisks();
        IReadOnlyList<VolumeInfo> volumes = VolumeEnumerator.EnumerateVolumes();

        _logger.LogDebug("Found {DiskCount} disk devices and {VolumeCount} volumes.", disks.Count, volumes.Count);

        var devices = new List<ExternalDevice>(disks.Count);

        foreach (DeviceInfo disk in disks)
        {
            var ownVolumes = volumes
                .Where(v => v.DiskNumbers.Contains(disk.DiskNumber))
                .ToList();

            if (!IsRelevant(disk, ownVolumes))
            {
                continue;
            }

            ExternalDevice device = Map(disk, ownVolumes);
            devices.Add(device);

            _logger.LogInformation(
                "Detected external device '{FriendlyName}' ({DeviceId}) on bus {BusType}, volume(s): {Letters}.",
                device.FriendlyName, device.DeviceId, device.BusType, string.Join(", ", device.DriveLetters));
        }

        return devices
            .OrderBy(d => d.DriveLetters.Count == 0 ? "~~" : string.Join("", d.DriveLetters))
            .ThenBy(d => d.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsRelevant(DeviceInfo disk, IReadOnlyCollection<VolumeInfo> ownVolumes)
    {
        BusType busType = NormalizeBusType(disk);

        // Ignore internal, virtual, optical and network storage.
        if (busType is BusType.Virtual or BusType.FileBackedVirtual or BusType.Spaces)
        {
            return false;
        }

        // Ignore system-era devices without a valid PhysicalDrive path.
        if (disk.DiskNumber < 0)
        {
            return false;
        }

        // Ignore empty reader slots and other no-media devices.
        if (!HasUsableMedia(disk, ownVolumes))
        {
            return false;
        }

        if (!DeviceClassifier.CanEject(disk.RemovalPolicy == 1, busType))
        {
            return false;
        }

        return true;
    }

    private static BusType NormalizeBusType(DeviceInfo disk)
    {
        BusType inferredBusType = InferBusType(
            disk.InstanceId,
            disk.BusReportedDeviceDesc,
            disk.FriendlyName,
            disk.Manufacturer);

        if (inferredBusType is BusType.Usb or BusType.Sd or BusType.IEEE1394)
        {
            return disk.BusType switch
            {
                BusType.Unknown or BusType.Scsi or BusType.Ata or BusType.Sata or BusType.Nvme => inferredBusType,
                _ => disk.BusType,
            };
        }

        return disk.BusType;
    }

    private static bool HasUsableMedia(DeviceInfo disk, IReadOnlyCollection<VolumeInfo> ownVolumes)
    {
        if (ownVolumes.Count > 0)
        {
            return true;
        }

        return (disk.CapacityBytes ?? 0UL) > 0;
    }

    private static BusType InferBusType(params string?[] candidates)
    {
        foreach (string? candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            string text = candidate.ToUpperInvariant();

            if (text.StartsWith("USBSTOR", StringComparison.Ordinal) ||
                text.StartsWith("USB\\", StringComparison.Ordinal) ||
                text.Contains(" USB ", StringComparison.Ordinal) ||
                text.Contains("USB", StringComparison.Ordinal))
            {
                return BusType.Usb;
            }

            if (text.StartsWith("SD\\", StringComparison.Ordinal) ||
                text.StartsWith("SDMMC", StringComparison.Ordinal) ||
                text.Contains("SD CARD", StringComparison.Ordinal) ||
                text.Contains("MICROSD", StringComparison.Ordinal))
            {
                return BusType.Sd;
            }

            if (text.StartsWith("1394", StringComparison.Ordinal) ||
                text.Contains("FIREWIRE", StringComparison.Ordinal))
            {
                return BusType.IEEE1394;
            }
        }

        return BusType.Unknown;
    }

    private static ExternalDevice Map(DeviceInfo disk, IReadOnlyList<VolumeInfo> ownVolumes)
    {
        VolumeInfo? primaryVolume = ownVolumes
            .OrderBy(v => v.IsSystemVolume ? 1 : 0)
            .ThenBy(v => v.Letters.FirstOrDefault() ?? "Z~")
            .FirstOrDefault();

        var letters = ownVolumes
            .SelectMany(v => v.Letters)
            .OrderBy(l => l)
            .ToList();

        (string? vendor, string? product) = DeviceIdParser.Parse(disk.InstanceId);

        string? friendlyName = disk.FriendlyName ?? disk.BusReportedDeviceDesc ?? product ?? disk.InstanceId;
        BusType busType = NormalizeBusType(disk);
        DeviceType deviceType = DeviceClassifier.Classify(busType, product ?? disk.BusReportedDeviceDesc);
        bool removable = disk.RemovalPolicy == 1;

        return new ExternalDevice
        {
            DeviceId = disk.InstanceId,
            FriendlyName = friendlyName,
            DriveLetters = letters,
            Vendor = vendor ?? disk.Manufacturer,
            Model = product,
            CapacityBytes = disk.CapacityBytes,
            BusType = busType,
            FileSystem = primaryVolume?.FileSystem,
            DiskNumber = disk.DiskNumber,
            DevicePath = disk.DevicePath,
            IsRemovable = removable,
            CanEject = DeviceClassifier.CanEject(removable, busType),
            DeviceType = deviceType,
            Icon = DeviceClassifier.IconFor(deviceType),
            ConnectedSince = disk.InstallDate,
        };
    }
}