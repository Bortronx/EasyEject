using System.Runtime.InteropServices;
using System.Text;
using EasyEject.Models;
using EasyEject.Win32.Interop;

namespace EasyEject.Win32;

/// <summary>
/// Performs safe device ejection using documented Windows APIs:
/// FSCTL_LOCK_VOLUME / FSCTL_DISMOUNT_VOLUME, IOCTL_STORAGE_EJECT_MEDIA and
/// CM_Request_Device_Eject.
/// </summary>
public static class DeviceEjector
{
    // CM_Request_Device_Eject flags (cfgmgr32.h)
    // The ulFlags parameter is not used and must be zero (Windows SDK docs).
    private const uint EjectFlagNone = 0x00000000;

    /// <summary>
    /// Safely ejects a single disk device. Retries on busy/veto.
    /// </summary>
    private const int BusyRetryDelayMs = 400;
    private const int MaxBusyRetries = 2;

    public static EjectOutcome Eject(DeviceInfo device, IReadOnlyList<VolumeInfo> ownVolumes)
    {
        // 1) Lock and dismount every non-system volume belonging to the disk.
        //    Retry once on Busy — PendingClose means handles are about to close.
        foreach (VolumeInfo volume in ownVolumes)
        {
            if (volume.IsSystemVolume)
            {
                continue;
            }

            LockVolumeResult lockResult = TryLockAndDismount(volume.VolumePath);
            if (lockResult == LockVolumeResult.Busy)
            {
                Thread.Sleep(BusyRetryDelayMs);
                lockResult = TryLockAndDismount(volume.VolumePath);
            }

            if (lockResult == LockVolumeResult.Busy)
            {
                return new EjectOutcome(false, EjectStatus.DeviceBusy, null, "The device is currently in use. Close any open files and try again.");
            }

            if (lockResult == LockVolumeResult.Removed)
            {
                return new EjectOutcome(false, EjectStatus.AlreadyRemoved, null, null);
            }
        }

        // 2) Ask the storage driver to eject the media (tolerant of failure —
        //    the PnP eject below is authoritative).
        TryEjectMedia(device.DevicePath);

        // 3) Ask the Plug and Play system to safely remove the device.
        //    Retry on busy/veto — Windows may need a moment to release handles.
        EjectOutcome outcome = RequestDeviceEject(device.InstanceId);
        for (int attempt = 0; attempt < MaxBusyRetries; attempt++)
        {
            if (outcome.RequestAccepted || outcome.Status == EjectStatus.AlreadyRemoved)
            {
                return outcome;
            }

            if (outcome.Status != EjectStatus.DeviceBusy && outcome.Status != EjectStatus.Vetoed)
            {
                break;
            }

            Thread.Sleep(BusyRetryDelayMs);
            outcome = RequestDeviceEject(device.InstanceId);
        }

        return outcome;
    }

    private static EjectOutcome RequestDeviceEject(string instanceId)
    {
        nuint devInst;
        uint locate = NativeMethods.CM_Locate_DevNodeW(out devInst, instanceId, 0);
        if (locate == Constants.CrNoSuchDevnode || locate == Constants.CrDeviceNotThere)
        {
            return new EjectOutcome(false, EjectStatus.AlreadyRemoved, null, null);
        }

        if (locate != Constants.CrSuccess)
        {
            return new EjectOutcome(false, EjectStatus.Failed, null, $"Locating the device failed (CONFIGRET 0x{locate:X8}).");
        }

        // Try the device itself first, then walk up the parent chain.
        // Windows "Safely Remove Hardware" ejects at the USB parent level,
        // which succeeds even when the disk child has open handles.
        EjectOutcome outcome = TryEjectDevNode(devInst);
        if (outcome.RequestAccepted || outcome.Status == EjectStatus.AlreadyRemoved)
        {
            return outcome;
        }

        nuint current = devInst;
        for (int depth = 0; depth < 6; depth++)
        {
            if (NativeMethods.CM_Get_Parent(out nuint parent, current, 0) != Constants.CrSuccess)
            {
                break;
            }

            outcome = TryEjectDevNode(parent);
            if (outcome.RequestAccepted || outcome.Status == EjectStatus.AlreadyRemoved)
            {
                return outcome;
            }

            current = parent;
        }

        // All attempts vetoed — return the last outcome.
        return outcome;
    }

    private static EjectOutcome TryEjectDevNode(nuint devInst)
    {
        var vetoName = new StringBuilder(256);
        PnpVetoType vetoType = PnpVetoType.Unknown;

        uint requestResult = NativeMethods.CM_Request_Device_EjectW(
            devInst, out vetoType, vetoName, (uint)vetoName.Capacity, EjectFlagNone);

        if (requestResult == Constants.CrSuccess)
        {
            return new EjectOutcome(true, EjectStatus.Success, null, null);
        }

        if (requestResult == Constants.CrNoSuchDevnode || requestResult == Constants.CrDeviceNotThere)
        {
            return new EjectOutcome(false, EjectStatus.AlreadyRemoved, null, null);
        }

        if (requestResult == Constants.CrRemoveVetoed)
        {
            string? veto = vetoName.Length > 0 ? vetoName.ToString() : null;
            return new EjectOutcome(false, MapVeto(vetoType), veto, DescribeVeto(vetoType));
        }

        return new EjectOutcome(false, EjectStatus.Failed, null, $"CONFIGRET 0x{requestResult:X8}");
    }

    private static EjectStatus MapVeto(PnpVetoType veto)
    {
        return veto switch
        {
            PnpVetoType.OutstandingOpen or PnpVetoType.DeviceBusy or PnpVetoType.PendingClose => EjectStatus.DeviceBusy,
            PnpVetoType.WindowsApp or PnpVetoType.DeviceInUse or PnpVetoType.DeviceEnumeration => EjectStatus.DeviceInUse,
            PnpVetoType.LegacyDevice => EjectStatus.NotRemovable,
            _ => EjectStatus.Vetoed,
        };
    }

    private static string? DescribeVeto(PnpVetoType veto)
    {
        return veto switch
        {
            PnpVetoType.OutstandingOpen => "Windows reported outstanding open handles.",
            PnpVetoType.DeviceBusy => "Windows reported the device is busy.",
            PnpVetoType.PendingClose => "A program is about to close file handles.",
            PnpVetoType.WindowsApp => "A Windows application is using the device.",
            PnpVetoType.DeviceInUse => "The device is currently in use.",
            PnpVetoType.DeviceEnumeration => "The device is still being enumerated.",
            PnpVetoType.LegacyDevice => "The device does not support safe removal.",
            _ => null,
        };
    }

    private static LockVolumeResult TryLockAndDismount(string volumePath)
    {
        using var handle = DiskDeviceEnumerator.OpenDevice(volumePath, Constants.GenericRead | Constants.GenericWrite, out int error);
        if (handle.IsInvalid)
        {
            return error switch
            {
                // Standard users cannot open volumes for locking; skip the
                // lock so the PnP eject can still be performed (it vetoes
                // when the device is genuinely busy).
                Constants.ErrorAccessDenied => LockVolumeResult.Ok,
                Constants.ErrorBusy => LockVolumeResult.Busy,
                Constants.ErrorDeviceRemoved => LockVolumeResult.Removed,
                _ => LockVolumeResult.Other,
            };
        }

        if (!NativeMethods.DeviceIoControl(handle, Constants.FsctlLockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
        {
            int lastError = Marshal.GetLastWin32Error();
            return lastError switch
            {
                Constants.ErrorAccessDenied or Constants.ErrorBusy => LockVolumeResult.Busy,
                Constants.ErrorNotSupported => LockVolumeResult.Ok,
                Constants.ErrorDeviceRemoved => LockVolumeResult.Removed,
                _ => LockVolumeResult.Other,
            };
        }

        // Dismount failure is not fatal; the device can still be ejected.
        NativeMethods.DeviceIoControl(handle, Constants.FsctlDismountVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);

        return LockVolumeResult.Ok;
    }

    private static void TryEjectMedia(string devicePath)
    {
        using var handle = DiskDeviceEnumerator.OpenDevice(devicePath, Constants.GenericRead | Constants.GenericWrite, out _);
        if (!handle.IsInvalid)
        {
            NativeMethods.DeviceIoControl(handle, Constants.IoctlStorageEjectMedia, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
        }
    }

    private enum LockVolumeResult
    {
        Ok,
        Busy,
        Removed,
        Other,
    }
}
