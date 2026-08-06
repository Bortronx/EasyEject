using System.Text;
using EasyEject.Win32.Interop;

namespace EasyEject.Win32;

/// <summary>
/// Enumerates mounted volumes using FindFirstVolume and maps them to drive
/// letters and owning disks through the volume mount point and WMI APIs, all
/// of which are accessible to standard (non-elevated) users.
/// </summary>
public static class VolumeEnumerator
{
    /// <summary>
    /// Enumerates every mounted volume on the local machine.
    /// </summary>
    /// <returns>The list of discovered volumes.</returns>
    public static IReadOnlyList<VolumeInfo> EnumerateVolumes()
    {
        IReadOnlyDictionary<string, IReadOnlyList<int>> diskNumbersByLetter = WmiDiskInfo.QueryVolumeDiskNumbers();

        var volumes = new List<VolumeInfo>();
        var buffer = new StringBuilder(512);

        // INVALID_HANDLE_VALUE. FindFirstVolumeW returns it when no volumes are found.
        IntPtr find = NativeMethods.FindFirstVolumeW(buffer, (uint)buffer.Capacity);
        if (find == new IntPtr(-1))
        {
            return volumes;
        }

        try
        {
            while (true)
            {
                string volumePath = buffer.ToString().TrimEnd('\\');
                volumes.Add(ReadVolumeInfo(volumePath, diskNumbersByLetter));

                if (!NativeMethods.FindNextVolumeW(find, buffer, (uint)buffer.Capacity))
                {
                    break;
                }
            }
        }
        finally
        {
            NativeMethods.FindVolumeClose(find);
        }

        return volumes
            .OrderBy(v => v.Letters.FirstOrDefault() ?? "~~")
            .ToList();
    }

    private static VolumeInfo ReadVolumeInfo(
        string volumePath,
        IReadOnlyDictionary<string, IReadOnlyList<int>> diskNumbersByLetter)
    {
        List<string> letters = GetLetters(volumePath);
        List<int> diskNumbers = letters
            .SelectMany(l => diskNumbersByLetter.TryGetValue(l, out IReadOnlyList<int>? numbers) ? numbers : Enumerable.Empty<int>())
            .Distinct()
            .OrderBy(i => i)
            .ToList();

        string? label = null;
        string? fileSystem = null;

        string? sampleLetter = letters.FirstOrDefault();
        if (sampleLetter is not null)
        {
            GetVolumeMetadata(sampleLetter, out label, out fileSystem);
        }

        return new VolumeInfo(volumePath, letters, diskNumbers, label, fileSystem);
    }

    private static List<string> GetLetters(string volumeGuid)
    {
        var letters = new List<string>();
        var volumeName = new StringBuilder(512);

        for (char drive = 'A'; drive <= 'Z'; drive++)
        {
            string mountPoint = $@"{drive}:\";
            if (NativeMethods.GetVolumeNameForVolumeMountPoint(mountPoint, volumeName, (uint)volumeName.Capacity))
            {
                string volumePath = volumeName.ToString().TrimEnd('\\');
                if (string.Equals(volumePath, volumeGuid, StringComparison.OrdinalIgnoreCase))
                {
                    letters.Add($@"{drive}:");
                }
            }
        }

        return letters;
    }

    private static void GetVolumeMetadata(string letter, out string? label, out string? fileSystem)
    {
        label = null;
        fileSystem = null;

        var labelBuffer = new StringBuilder(256);
        var fsBuffer = new StringBuilder(32);

        if (NativeMethods.GetVolumeInformation(
                letter + "\\",
                labelBuffer, labelBuffer.Capacity,
                out _, out _, out _,
                fsBuffer, fsBuffer.Capacity))
        {
            label = labelBuffer.Length > 0 ? labelBuffer.ToString() : null;
            fileSystem = fsBuffer.Length > 0 ? fsBuffer.ToString() : null;
        }
    }
}