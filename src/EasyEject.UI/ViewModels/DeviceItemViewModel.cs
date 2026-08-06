using CommunityToolkit.Mvvm.ComponentModel;
using EasyEject.Core.Services;
using EasyEject.Models;

namespace EasyEject.UI.ViewModels;

/// <summary>
/// A selectable theme option shown in the theme picker.
/// </summary>
/// <param name="Label">The display label.</param>
/// <param name="Mode">The theme mode.</param>
public sealed record ThemeOption(string Label, ThemeMode Mode);

/// <summary>
/// View model for a single device card in the device list.
/// </summary>
public sealed partial class DeviceItemViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    [NotifyPropertyChangedFor(nameof(DetailText))]
    [NotifyPropertyChangedFor(nameof(CapacityText))]
    [NotifyPropertyChangedFor(nameof(VolumeText))]
    [NotifyPropertyChangedFor(nameof(FileSystemText))]
    [NotifyPropertyChangedFor(nameof(ConnectedSinceText))]
    [NotifyPropertyChangedFor(nameof(IconGlyph))]
    public partial ExternalDevice Device { get; set; }

    /// <summary>
    /// Gets or sets whether the radio selection is active.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceItemViewModel"/> class.
    /// </summary>
    /// <param name="device">The wrapped device.</param>
    public DeviceItemViewModel(ExternalDevice device)
    {
        Device = device;
    }

    /// <summary>
    /// Gets the icon glyph for the device.
    /// </summary>
    public string IconGlyph => Device.Icon switch
    {
        DeviceIcon.SolidStateDrive or DeviceIcon.HardDrive => "\uEDA2",
        DeviceIcon.MemoryCard => "\uE7F1",
        DeviceIcon.Usb => "\uE88E",
        _ => "\uE9CE",
    };

    /// <summary>
    /// Gets the primary display name.
    /// </summary>
    public string Title => Device.FriendlyName ?? Device.Model ?? "Unknown device";

    /// <summary>
    /// Gets the secondary line: type and bus.
    /// </summary>
    public string Subtitle => $"{EnumTexts.DeviceType(Device.DeviceType)} · {EnumTexts.BusType(Device.BusType)}";

    /// <summary>
    /// Gets the detail line: vendor, model and file system.
    /// </summary>
    public string DetailText
    {
        get
        {
            var parts = new[] { Device.Vendor, Device.Model, Device.FileSystem }
                .Where(x => !string.IsNullOrWhiteSpace(x));
            string joined = string.Join(" · ", parts);
            return string.IsNullOrEmpty(joined) ? "—" : joined;
        }
    }

    /// <summary>
    /// Gets the formatted capacity.
    /// </summary>
    public string CapacityText => CapacityFormatter.Format(Device.CapacityBytes);

    /// <summary>
    /// Gets the drive letter text.
    /// </summary>
    public string VolumeText => EnumTexts.DriveLetters(Device.DriveLetters);

    /// <summary>
    /// Gets the file system text.
    /// </summary>
    public string FileSystemText => Device.FileSystem ?? "—";

    /// <summary>
    /// Gets the connected-since text.
    /// </summary>
    public string ConnectedSinceText => Device.ConnectedSince?.ToLocalTime().ToString("g") ?? "—";

    /// <summary>
    /// Gets whether the device can be ejected.
    /// </summary>
    public bool CanEject => Device.CanEject;

    /// <summary>
    /// Refreshes the wrapped device and raises change notifications.
    /// </summary>
    /// <param name="device">The updated device.</param>
    public void Update(ExternalDevice device)
    {
        Device = device;
    }
}