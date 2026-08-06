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
            if (!IsRelevant(disk))
            {
                continue;
            }

            var ownVolumes = volumes
                .Where(v => v.DiskNumbers.Contains(disk.DiskNumber))
                .ToList();

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

    private static bool IsRelevant(DeviceInfo disk)
    {
        // Ignore internal, virtual, optical and network storage.
        if (disk.BusType is BusType.Virtual or BusType.FileBackedVirtual or BusType.Spaces)
        {
            return false;
        }

        // Ignore system-era devices without a valid PhysicalDrive path.
        if (disk.DiskNumber < 0)
        {
            return false;
        }

        if (!DeviceClassifier.CanEject(disk.RemovalPolicy == 1, disk.BusType))
        {
            return false;
        }

        return true;
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
        BusType busType = disk.BusType;
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