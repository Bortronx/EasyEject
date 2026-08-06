using System.Runtime.InteropServices;

namespace EasyEject.Win32.Interop;

/// <summary>
/// Interop structures and the safe handle used by the Win32 layer.
/// </summary>

/// <summary>
/// Safe handle wrapper for CreateFile handles.
/// </summary>
public sealed class SafeDeviceHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeDeviceHandle()
        : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle() => CloseHandle(handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}

/// <summary>
/// SP_DEVICE_INTERFACE_DATA (setupapi.h).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SpDeviceInterfaceData
{
    public uint CbSize;
    public Guid InterfaceClassGuid;
    public uint Flags;
    public nuint Reserved;
}

/// <summary>
/// SP_DEVINFO_DATA (setupapi.h).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SpDeviceInfoData
{
    public uint CbSize;
    public Guid ClassGuid;
    public uint DevInst;
    public nuint Reserved;
}

/// <summary>
/// SP_DEVICE_INTERFACE_DETAIL_DATA_W (setupapi.h).
/// The <see cref="DevicePath"/> field is the first character of an inline
/// variable-length string that is read directly from the native buffer.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct SpDeviceInterfaceDetailData
{
    public uint CbSize;
    public char DevicePath;
}

/// <summary>
/// DEVPROPKEY (devpropdef.h): a GUID plus a property identifier.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DevPropKey
{
    public Guid FmtId;
    public ulong Pid;
}
