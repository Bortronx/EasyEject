using EasyEject.Models;

namespace EasyEject.Win32;

/// <summary>
/// Raw device information collected from the Windows device tree.
/// </summary>
public sealed record DeviceInfo(
    string InstanceId,
    string? FriendlyName,
    string? Manufacturer,
    string DevicePath,
    int DiskNumber,
    BusType BusType,
    uint RemovalPolicy,
    ulong? CapacityBytes,
    DateTimeOffset? InstallDate,
    string? BusReportedDeviceDesc);

/// <summary>
/// A mounted volume with its drive letters and owning disk numbers.
/// </summary>
public sealed record VolumeInfo(
    string VolumePath,
    IReadOnlyList<string> Letters,
    IReadOnlyList<int> DiskNumbers,
    string? Label,
    string? FileSystem)
{
    /// <summary>
    /// Gets a value indicating whether this volume is the Windows system drive.
    /// </summary>
    public bool IsSystemVolume
    {
        get
        {
            string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
            return Letters.Any(l => string.Equals(l, systemDrive.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(l + "\\", systemDrive, StringComparison.OrdinalIgnoreCase));
        }
    }
}

/// <summary>
/// The outcome of a low-level eject request.
/// </summary>
public sealed record EjectOutcome(bool RequestAccepted, EjectStatus Status, string? VetoName, string? Detail);
