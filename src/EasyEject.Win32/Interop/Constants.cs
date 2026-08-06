using System.Runtime.InteropServices;

namespace EasyEject.Win32.Interop;

/// <summary>
/// Win32 GUIDs, IOCTL codes and flags used by EasyEject.
/// All values come from the Windows SDK headers.
/// </summary>
internal static class Constants
{
    // Device interface / setup class GUIDs (devguid.h)
    public static readonly Guid GuidDevInterfaceDisk = new("53f56307-b6bf-11d0-94f2-00a0c91efb8b");

    // Device property keys (devpkey.h)
    public static readonly Guid DevPropKeyInstallGuid = new("83da6326-97a6-4088-9453-a1923f573b29");
    public static readonly Guid DevPropKeyBusReportedGuid = new("540b947e-8b40-45bc-a8a2-6a0b894cbda2");

    public const uint DevPropKeyInstallDate = 100;
    public const uint DevPropKeyBusReportedDeviceDesc = 4;

    // SetupDiGetClassDevs flags (setupapi.h)
    public const uint DigcfPresent = 0x00000002;
    public const uint DigcfDeviceInterface = 0x00000010;

    // SetupDiGetDeviceRegistryProperty property identifiers (setupapi.h)
    public const uint SpdrpDeviceDesc = 0x00000000;
    public const uint SpdrpHardwareId = 0x00000001;
    public const uint SpdrpManufacturer = 0x0000000B;
    public const uint SpdrpFriendlyName = 0x0000000C;
    public const uint SpdrpRemovalPolicy = 0x0000001B;

    // STORAGE_PROPERTY_ID / STORAGE_QUERY_TYPE (ntddstor.h)
    public const uint StorageDeviceProperty = 0;
    public const uint PropertyStandardQuery = 0;

    // CTL_CODE(deviceType, function, method, access) = (deviceType << 16) | (access << 14) | (function << 2) | method
    public const uint MethodBuffered = 0;
    public const uint FileReadAccess = 0x00000001;
    public const uint FileAnyAccess = 0;

    public const uint IoctlStorageEjectMedia = 0x002D0000 | (FileReadAccess << 14) | (0x0202 << 2) | MethodBuffered;
    public const uint IoctlStorageQueryProperty = 0x002D0000 | (FileAnyAccess << 14) | (0x0500 << 2) | MethodBuffered;
    public const uint FsctlLockVolume = 0x00090000 | (FileAnyAccess << 14) | (6 << 2) | MethodBuffered;
    public const uint FsctlDismountVolume = 0x00090000 | (FileAnyAccess << 14) | (8 << 2) | MethodBuffered;

    // CreateFile access / share / creation flags (winnt.h, winbase.h)
    public const uint GenericRead = 0x80000000;
    public const uint GenericWrite = 0x40000000;
    public const uint FileShareRead = 0x00000001;
    public const uint FileShareWrite = 0x00000002;
    public const uint FileShareDelete = 0x00000004;
    public const uint OpenExisting = 3;
    public const uint FileAttributeNormal = 0x00000080;

    // Windows error codes (winerror.h)
    public const int ErrorAccessDenied = 5;
    public const int ErrorBusy = 170;
    public const int ErrorNotSupported = 50;
    public const int ErrorNoMoreItems = 259;
    public const int ErrorMoreData = 234;
    public const int ErrorInsufficientBuffer = 122;
    public const int ErrorDeviceRemoved = 1617;

    // CONFIGRET values (cfgmgr32.h)
    public const uint CrSuccess = 0x00000000;
    public const uint CrNoSuchDevnode = 0x0000000D;
    public const uint CrRemoveVetoed = 0x00000017;
    public const uint CrDeviceNotThere = 0x00000024;
}

/// <summary>
/// PNP veto types returned by CM_Request_Device_Eject (pnp.h).
/// </summary>
internal enum PnpVetoType
{
    Unknown = 0,
    LegacyDevice = 1,
    PendingClose = 2,
    WindowsApp = 3,
    OutstandingOpen = 4,
    DeviceEnumeration = 5,
    DeviceBusy = 6,
    DeviceInUse = 7,
}

/// <summary>
/// Input structure for IOCTL_STORAGE_QUERY_PROPERTY.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct StoragePropertyQuery
{
    public uint PropertyId;
    public uint QueryType;
    public byte AdditionalParameters;
}
