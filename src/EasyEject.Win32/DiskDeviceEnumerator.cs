using System.Runtime.InteropServices;
using System.Text;
using EasyEject.Models;
using EasyEject.Win32.Interop;

namespace EasyEject.Win32;

/// <summary>
/// Enumerates disk devices on the local machine using the documented SetupAPI
/// and Configuration Manager APIs (GUID_DEVINTERFACE_DISK).
/// </summary>
public static class DiskDeviceEnumerator
{
    /// <summary>
    /// Enumerates every disk device and collects its properties.
    /// </summary>
    /// <returns>The list of discovered disks.</returns>
    public static IReadOnlyList<DeviceInfo> EnumerateDisks()
    {
        IReadOnlyDictionary<string, (int Index, ulong? Size)> diskInfoById = WmiDiskInfo.QueryDisks();
        var result = new List<DeviceInfo>();

        IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevsW(
            in Constants.GuidDevInterfaceDisk, null, IntPtr.Zero,
            Constants.DigcfPresent | Constants.DigcfDeviceInterface);

        if (deviceInfoSet == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            if (error == Constants.ErrorNoMoreItems)
            {
                return result;
            }

            throw new Win32ApiException("SetupDiGetClassDevsW", error);
        }

        try
        {
            uint index = 0;
            while (true)
            {
                var interfaceData = new SpDeviceInterfaceData { CbSize = (uint)Marshal.SizeOf<SpDeviceInterfaceData>() };

                if (!NativeMethods.SetupDiEnumDeviceInterfaces(
                        deviceInfoSet, IntPtr.Zero, in Constants.GuidDevInterfaceDisk, index, ref interfaceData))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == Constants.ErrorNoMoreItems)
                    {
                        break;
                    }

                    throw new Win32ApiException("SetupDiEnumDeviceInterfaces", error);
                }

                index++;

                var deviceInfoData = new SpDeviceInfoData { CbSize = (uint)Marshal.SizeOf<SpDeviceInfoData>() };
                string? path = GetInterfaceDetailPath(deviceInfoSet, ref interfaceData, ref deviceInfoData);
                if (path is null)
                {
                    continue;
                }

                var disk = ReadDiskInfo(deviceInfoSet, ref deviceInfoData, path, diskInfoById);
                if (disk is not null)
                {
                    result.Add(disk);
                }
            }
        }
        finally
        {
            NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return result;
    }

    private static string? GetInterfaceDetailPath(
        IntPtr deviceInfoSet, ref SpDeviceInterfaceData interfaceData, ref SpDeviceInfoData deviceInfoData)
    {
        // First call: obtain the required buffer size.
        // The documented pattern is that this call fails with
        // ERROR_INSUFFICIENT_BUFFER and returns the required size in
        // RequiredSize (see SetupDiGetDeviceInterfaceDetailW documentation).
        if (!NativeMethods.SetupDiGetDeviceInterfaceDetailW(
                deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out uint requiredSize, ref deviceInfoData))
        {
            int error = Marshal.GetLastWin32Error();
            if (error != Constants.ErrorInsufficientBuffer || requiredSize == 0)
            {
                if (error == Constants.ErrorNoMoreItems)
                {
                    return null;
                }

                throw new Win32ApiException("SetupDiGetDeviceInterfaceDetailW", error);
            }
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            // cbSize must equal Marshal.SizeOf<SpDeviceInterfaceDetailData> (8 bytes):
            // the native struct (DWORD + WCHAR[1]) is padded to a 4-byte multiple.
            Marshal.WriteInt32(buffer, Marshal.SizeOf<SpDeviceInterfaceDetailData>());

            if (!NativeMethods.SetupDiGetDeviceInterfaceDetailW(
                    deviceInfoSet, ref interfaceData, buffer, requiredSize, out _, ref deviceInfoData))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == Constants.ErrorNoMoreItems)
                {
                    return null;
                }

                throw new Win32ApiException("SetupDiGetDeviceInterfaceDetailW", error);
            }

            IntPtr pathPointer = buffer + Marshal.OffsetOf<SpDeviceInterfaceDetailData>("DevicePath");
            return Marshal.PtrToStringUni(pathPointer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static DeviceInfo? ReadDiskInfo(
        IntPtr deviceInfoSet,
        ref SpDeviceInfoData deviceInfoData,
        string devicePath,
        IReadOnlyDictionary<string, (int Index, ulong? Size)> diskInfoById)
    {
        string? instanceId = GetDeviceInstanceId(deviceInfoSet, ref deviceInfoData);
        if (instanceId is null)
        {
            return null;
        }

        string? friendlyName = GetRegistryString(deviceInfoSet, ref deviceInfoData, Constants.SpdrpFriendlyName);
        string? manufacturer = GetRegistryString(deviceInfoSet, ref deviceInfoData, Constants.SpdrpManufacturer);
        uint removalPolicy = GetRegistryUInt32(deviceInfoSet, ref deviceInfoData, Constants.SpdrpRemovalPolicy);

        nuint devInst = deviceInfoData.DevInst;

        (BusType busType, string? busReported) = GetBusType(devInst, devicePath);

        // The WMI disk index is authoritative and is resolvable for standard users.
        (int diskNumber, ulong? capacity) = diskInfoById.TryGetValue(instanceId, out var info)
            ? (info.Index, info.Size)
            : (-1, null);

        // Use the PhysicalDrive path when the disk number is known so that the
        // eject flow targets the real storage device.
        devicePath = diskNumber >= 0 ? $@"\\.\PhysicalDrive{diskNumber}" : devicePath;

        DateTimeOffset? installDate = GetDevNodePropertyFileTime(devInst, Constants.DevPropKeyInstallGuid, Constants.DevPropKeyInstallDate);

        return new DeviceInfo(
            instanceId,
            friendlyName,
            manufacturer,
            devicePath,
            diskNumber,
            busType,
            removalPolicy,
            capacity,
            installDate,
            busReported);
    }

    private static string? GetDeviceInstanceId(IntPtr deviceInfoSet, ref SpDeviceInfoData deviceInfoData)
    {
        var buffer = new StringBuilder(512);
        if (NativeMethods.SetupDiGetDeviceInstanceIdW(deviceInfoSet, ref deviceInfoData, buffer, (uint)buffer.Capacity, out _))
        {
            return buffer.ToString();
        }

        // Fallback for drivers that do not expose an instance ID.
        return GetRegistryString(deviceInfoSet, ref deviceInfoData, Constants.SpdrpHardwareId)
               ?? GetRegistryString(deviceInfoSet, ref deviceInfoData, Constants.SpdrpDeviceDesc);
    }

    private static (BusType BusType, string? BusReported) GetBusType(nuint devInst, string devicePath)
    {
        // 1) The STORAGE_DEVICE_DESCRIPTOR provides an authoritative bus type.
        BusType descriptorBus = QueryStorageBusType(devicePath);
        if (descriptorBus != BusType.Unknown)
        {
            return (descriptorBus, GetDevNodePropertyString(devInst, Constants.DevPropKeyBusReportedGuid, Constants.DevPropKeyBusReportedDeviceDesc));
        }

        // 2) Fall back to the parent device chain (documented CM_* APIs).
        string? instanceId = GetDevNodeInstanceId(devInst);
        if (instanceId is not null)
        {
            BusType fromId = BusTypeFromInstanceId(instanceId);
            if (fromId != BusType.Unknown)
            {
                return (fromId, GetDevNodePropertyString(devInst, Constants.DevPropKeyBusReportedGuid, Constants.DevPropKeyBusReportedDeviceDesc));
            }
        }

        // 3) Walk up the device tree looking for the transport device.
        BusType fromParent = BusTypeFromParentChain(devInst);
        if (fromParent != BusType.Unknown)
        {
            return (fromParent, GetDevNodePropertyString(devInst, Constants.DevPropKeyBusReportedGuid, Constants.DevPropKeyBusReportedDeviceDesc));
        }

        return (BusType.Unknown, GetDevNodePropertyString(devInst, Constants.DevPropKeyBusReportedGuid, Constants.DevPropKeyBusReportedDeviceDesc));
    }

    private static BusType BusTypeFromInstanceId(string instanceId)
    {
        string upper = instanceId.ToUpperInvariant();
        if (upper.StartsWith("USBSTOR", StringComparison.Ordinal) || upper.StartsWith("USB", StringComparison.Ordinal))
        {
            return BusType.Usb;
        }

        if (upper.StartsWith("SD\\", StringComparison.Ordinal) || upper.StartsWith("SDMMC", StringComparison.Ordinal))
        {
            return BusType.Sd;
        }

        if (upper.StartsWith("SCSI", StringComparison.Ordinal))
        {
            return BusType.Scsi;
        }

        if (upper.StartsWith("IDE", StringComparison.Ordinal))
        {
            return BusType.Ata;
        }

        if (upper.StartsWith("PCI", StringComparison.Ordinal) || upper.Contains("NVME", StringComparison.Ordinal))
        {
            return BusType.Nvme;
        }

        return BusType.Unknown;
    }

    private static BusType BusTypeFromParentChain(nuint devInst)
    {
        nuint current = devInst;
        for (int depth = 0; depth < 6; depth++)
        {
            if (NativeMethods.CM_Get_Parent(out nuint parent, current, 0) != Constants.CrSuccess)
            {
                break;
            }

            string? id = GetDevNodeInstanceId(parent);
            if (id is not null)
            {
                BusType bus = BusTypeFromInstanceId(id);
                if (bus != BusType.Unknown)
                {
                    return bus;
                }
            }

            current = parent;
        }

        return BusType.Unknown;
    }

    private static string? GetDevNodeInstanceId(nuint devInst)
    {
        var buffer = new StringBuilder(512);
        uint result = NativeMethods.CM_Get_Device_IDW(devInst, buffer, (uint)buffer.Capacity, 0);
        return result == Constants.CrSuccess ? buffer.ToString() : null;
    }

    private static string? GetDevNodePropertyString(nuint devInst, Guid keyGuid, ulong pid)
    {
        var key = new DevPropKey { FmtId = keyGuid, Pid = pid };
        uint bufferSize = 0;

        uint status = NativeMethods.CM_Get_DevNode_PropertyW(devInst, ref key, out _, IntPtr.Zero, ref bufferSize, 0);
        if (status != Constants.CrSuccess || bufferSize == 0)
        {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            status = NativeMethods.CM_Get_DevNode_PropertyW(devInst, ref key, out _, buffer, ref bufferSize, 0);
            return status == Constants.CrSuccess ? Marshal.PtrToStringUni(buffer) : null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static DateTimeOffset? GetDevNodePropertyFileTime(nuint devInst, Guid keyGuid, ulong pid)
    {
        var key = new DevPropKey { FmtId = keyGuid, Pid = pid };
        uint bufferSize = 8;

        IntPtr buffer = Marshal.AllocHGlobal(8);
        try
        {
            uint status = NativeMethods.CM_Get_DevNode_PropertyW(devInst, ref key, out _, buffer, ref bufferSize, 0);
            if (status != Constants.CrSuccess || bufferSize != 8)
            {
                return null;
            }

            long fileTime = Marshal.ReadInt64(buffer);
            if (fileTime == 0)
            {
                return null;
            }

            return DateTimeOffset.FromFileTime(fileTime).ToUniversalTime();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? GetRegistryString(IntPtr deviceInfoSet, ref SpDeviceInfoData deviceInfoData, uint property)
    {
        uint required = 0;
        if (!NativeMethods.SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet, ref deviceInfoData, property, out _, IntPtr.Zero, 0, out required))
        {
            int error = Marshal.GetLastWin32Error();
            if (error is not (Constants.ErrorMoreData or Constants.ErrorInsufficientBuffer))
            {
                return null;
            }
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)required);
        try
        {
            if (!NativeMethods.SetupDiGetDeviceRegistryPropertyW(
                    deviceInfoSet, ref deviceInfoData, property, out _, buffer, required, out _))
            {
                return null;
            }

            return Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static uint GetRegistryUInt32(IntPtr deviceInfoSet, ref SpDeviceInfoData deviceInfoData, uint property)
    {
        IntPtr buffer = Marshal.AllocHGlobal(4);
        try
        {
            if (!NativeMethods.SetupDiGetDeviceRegistryPropertyW(
                    deviceInfoSet, ref deviceInfoData, property, out _, buffer, 4, out _))
            {
                return 0;
            }

            return (uint)Marshal.ReadInt32(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Queries STORAGE_DEVICE_DESCRIPTOR to obtain the storage bus type.
    /// </summary>
    public static BusType QueryStorageBusType(string devicePath)
    {
        using var handle = OpenDevice(devicePath, Constants.FileReadAccess, out int error);
        if (handle.IsInvalid)
        {
            return BusType.Unknown;
        }

        // STORAGE_PROPERTY_QUERY (8 bytes) + room for the descriptor.
        int inputSize = 8;
        int outputSize = 512;
        IntPtr input = Marshal.AllocHGlobal(inputSize);
        IntPtr output = Marshal.AllocHGlobal(outputSize);
        try
        {
            var query = new StoragePropertyQuery
            {
                PropertyId = Constants.StorageDeviceProperty,
                QueryType = Constants.PropertyStandardQuery,
            };
            Marshal.StructureToPtr(query, input, false);

            if (!NativeMethods.DeviceIoControl(
                    handle, Constants.IoctlStorageQueryProperty,
                    input, (uint)inputSize, output, (uint)outputSize, out _, IntPtr.Zero))
            {
                return BusType.Unknown;
            }

            // STORAGE_DEVICE_DESCRIPTOR layout: BusType at offset 28.
            return (BusType)Marshal.ReadInt32(output, 28);
        }
        finally
        {
            Marshal.FreeHGlobal(input);
            Marshal.FreeHGlobal(output);
        }
    }

    /// <summary>
    /// Opens a device such as <c>\\.\PhysicalDrive2</c> or <c>\\.\Volume{...}</c>.
    /// </summary>
    /// <returns>The device handle, or an invalid handle when <paramref name="error"/> is set.</returns>
    public static SafeDeviceHandle OpenDevice(string devicePath, uint access, out int error)
    {
        SafeDeviceHandle handle = NativeMethods.CreateFile(
            devicePath,
            access,
            Constants.FileShareRead | Constants.FileShareWrite | Constants.FileShareDelete,
            IntPtr.Zero,
            Constants.OpenExisting,
            Constants.FileAttributeNormal,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            error = Marshal.GetLastWin32Error();
        }
        else
        {
            error = 0;
        }

        return handle;
    }
}
