using System.Runtime.InteropServices;
using System.Text;

namespace EasyEject.Win32.Interop;

/// <summary>
/// Native P/Invoke declarations for the Windows APIs used by EasyEject.
/// Signatures mirror the Windows SDK headers (kernel32, setupapi, cfgmgr32, ntdll).
/// </summary>
internal static partial class NativeMethods
{
    private const string Kernel32 = "kernel32.dll";
    private const string SetupApi = "setupapi.dll";
    private const string CfgMgr32 = "cfgmgr32.dll";

    // ---------------------------------------------------------------
    // Kernel32
    // ---------------------------------------------------------------

    /// <summary>Creates or opens a file or device.</summary>
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeDeviceHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    /// <summary>Sends a control code directly to the specified device driver.</summary>
    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeDeviceHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    /// <summary>Retrieves information about the specified volume's file system and root directory.</summary>
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumeInformation(
        string lpRootPathName,
        StringBuilder lpVolumeNameBuffer,
        int nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        out uint lpMaximumComponentLength,
        out uint lpFileSystemFlags,
        StringBuilder lpFileSystemNameBuffer,
        int nFileSystemNameSize);

    /// <summary>Retrieves the volume GUID path for the specified volume mount point.</summary>
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumeNameForVolumeMountPoint(
        string lpszVolumeMountPoint,
        StringBuilder lpszVolumeName,
        uint cchBufferLength);

    /// <summary>Begins enumerating the volumes on the local machine.</summary>
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindFirstVolumeW(StringBuilder lpszVolumeName, uint cchBufferLength);

    /// <summary>Continues enumerating the volumes on the local machine.</summary>
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FindNextVolumeW(IntPtr hFindVolume, StringBuilder lpszVolumeName, uint cchBufferLength);

    /// <summary>Closes a volume enumeration handle.</summary>
    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FindVolumeClose(IntPtr hFindVolume);

    // ---------------------------------------------------------------
    // SetupAPI
    // ---------------------------------------------------------------

    /// <summary>Returns a device information set that contains specified device information elements.</summary>
    [DllImport(SetupApi, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr SetupDiGetClassDevsW(
        in Guid classGuid,
        string? enumerator,
        IntPtr hwndParent,
        uint flags);

    /// <summary>Destroys a device information set and frees associated memory.</summary>
    [DllImport(SetupApi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    /// <summary>Returns information about a device interface in a device information set.</summary>
    [DllImport(SetupApi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet,
        IntPtr deviceInfoData,
        in Guid interfaceClassGuid,
        uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    /// <summary>Retrieves details for a device interface.</summary>
    [DllImport(SetupApi, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInterfaceDetailW(
        IntPtr deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        ref SpDeviceInfoData deviceInfoData);

    /// <summary>Retrieves a specified Plug and Play device property.</summary>
    [DllImport(SetupApi, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceRegistryPropertyW(
        IntPtr deviceInfoSet,
        ref SpDeviceInfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        IntPtr propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    /// <summary>Retrieves the device instance ID of a device information element.</summary>
    [DllImport(SetupApi, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInstanceIdW(
        IntPtr deviceInfoSet,
        ref SpDeviceInfoData deviceInfoData,
        StringBuilder deviceInstanceId,
        uint deviceInstanceIdSize,
        out uint requiredSize);

    /// <summary>Retrieves the device instance ID of a device information element.</summary>
    [DllImport(CfgMgr32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_DevNode_PropertyW(
        nuint dnDevInst,
        ref DevPropKey propertyKey,
        out uint propertyType,
        IntPtr propertyBuffer,
        ref uint propertyBufferSize,
        uint ulFlags);

    /// <summary>Retrieves the device instance ID of a device instance.</summary>
    [DllImport(CfgMgr32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_Device_IDW(
        nuint dnDevInst,
        StringBuilder buffer,
        uint bufferLen,
        uint ulFlags);

    /// <summary>Locates a device instance by its device instance ID.</summary>
    [DllImport(CfgMgr32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint CM_Locate_DevNodeW(
        out nuint pdnDevInst,
        string? pDeviceID,
        uint ulFlags);

    /// <summary>Retrieves the parent device instance of the specified device instance.</summary>
    [DllImport(CfgMgr32, SetLastError = true)]
    public static extern uint CM_Get_Parent(
        out nuint pdnDevInst,
        nuint dnDevInst,
        uint ulFlags);

    /// <summary>Requests that a device be ejected (safe removal).</summary>
    [DllImport(CfgMgr32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint CM_Request_Device_EjectW(
        nuint dnDevInst,
        out PnpVetoType pVetoType,
        StringBuilder pszVetoName,
        uint ulNameLength,
        uint ulFlags);
}
