namespace EasyEject.Models;

/// <summary>
/// Describes an external (removable) storage device detected on this computer.
/// </summary>
public sealed class ExternalDevice : IEquatable<ExternalDevice>
{
    /// <summary>
    /// Gets or sets the device instance ID (for example
    /// <c>USBSTOR\DISK&amp;VEN_KINGSTON&amp;PROD_DATATRAVELER\…</c>).
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets or sets the friendly display name reported by the driver.
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// Gets or sets the mount point letters, for example <c>D:</c>, <c>E:</c>.
    /// </summary>
    public IReadOnlyList<string> DriveLetters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the vendor name parsed from the device ID.
    /// </summary>
    public string? Vendor { get; init; }

    /// <summary>
    /// Gets or sets the product/model name parsed from the device ID.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets or sets the total capacity in bytes, when it can be determined.
    /// </summary>
    public ulong? CapacityBytes { get; init; }

    /// <summary>
    /// Gets or sets the storage bus the device is attached to.
    /// </summary>
    public BusType BusType { get; init; }

    /// <summary>
    /// Gets or sets the file system of the first mounted volume, if any.
    /// </summary>
    public string? FileSystem { get; init; }

    /// <summary>
    /// Gets or sets the zero-based Windows disk number (<c>PhysicalDriveN</c>).
    /// </summary>
    public int DiskNumber { get; init; }

    /// <summary>
    /// Gets or sets the fully-qualified device path, for example <c>\\.\PhysicalDrive2</c>.
    /// </summary>
    public string? DevicePath { get; init; }

    /// <summary>
    /// Gets or sets whether Windows considers this device removable.
    /// </summary>
    public bool IsRemovable { get; init; }

    /// <summary>
    /// Gets or sets whether the device can be ejected by this application.
    /// </summary>
    public bool CanEject { get; init; }

    /// <summary>
    /// Gets or sets the human readable device classification.
    /// </summary>
    public DeviceType DeviceType { get; init; }

    /// <summary>
    /// Gets or sets the icon used to represent the device.
    /// </summary>
    public DeviceIcon Icon { get; init; }

    /// <summary>
    /// Gets or sets the UTC date the device was first connected (when available).
    /// </summary>
    public DateTimeOffset? ConnectedSince { get; init; }

    /// <inheritdoc />
    public bool Equals(ExternalDevice? other) =>
        other is not null && string.Equals(DeviceId, other.DeviceId, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ExternalDevice other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(DeviceId);
}