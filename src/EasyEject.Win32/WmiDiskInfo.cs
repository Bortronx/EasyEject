using System.Management;
using System.Text.RegularExpressions;

namespace EasyEject.Win32;

/// <summary>
/// Queries disk numbering, capacity and volume-to-disk mapping through WMI
/// (CIMV2). Unlike device-path based IOCTLs, WMI queries remain accessible
/// to standard (non-elevated) users.
/// </summary>
internal static class WmiDiskInfo
{
    private static readonly Regex LogicalDiskRef = new(@"Win32_LogicalDisk\.DeviceID=""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex PartitionRef = new(@"Win32_DiskPartition\.DeviceID=""([^""]+)""", RegexOptions.Compiled);

    /// <summary>
    /// Returns the disk index and capacity for every physical disk, keyed by
    /// the PnP device instance ID (case-insensitive).
    /// </summary>
    public static IReadOnlyDictionary<string, (int Index, ulong? Size)> QueryDisks()
    {
        var result = new Dictionary<string, (int Index, ulong? Size)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT PNPDeviceID, Index, Size FROM Win32_DiskDrive");
            foreach (ManagementObject disk in searcher.Get().Cast<ManagementObject>())
            {
                string? pnpId = disk["PNPDeviceID"] as string;
                if (string.IsNullOrWhiteSpace(pnpId))
                {
                    continue;
                }

                int index = Convert.ToInt32(disk["Index"] ?? -1);
                ulong? size = disk["Size"] is null ? null : Convert.ToUInt64(disk["Size"]);
                result[pnpId] = (index, size);
            }
        }
        catch (Exception)
        {
            // Enumeration must never fail; an empty map simply means the
            // disks are reported without numbers.
        }

        return result;
    }

    /// <summary>
    /// Returns the owning disk indices for every logical drive letter
    /// (e.g. "C:"), derived from the Win32_LogicalDisk → partition → disk
    /// associations.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<int>> QueryVolumeDiskNumbers()
    {
        var partitionToDisk = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var letterToPartitions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using (var partitions = new ManagementObjectSearcher("SELECT DeviceID, DiskIndex FROM Win32_DiskPartition"))
            {
                foreach (ManagementObject partition in partitions.Get().Cast<ManagementObject>())
                {
                    string? deviceId = partition["DeviceID"] as string;
                    if (deviceId is not null)
                    {
                        partitionToDisk[deviceId] = Convert.ToInt32(partition["DiskIndex"] ?? -1);
                    }
                }
            }

            using (var associations = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition"))
            {
                foreach (ManagementObject row in associations.Get().Cast<ManagementObject>())
                {
                    // Win32_LogicalDiskToPartition: Antecedent = the partition,
                    // Dependent = the logical disk.
                    string? partitionId = ExtractKey(row["Antecedent"] as string, PartitionRef);
                    string? letter = ExtractKey(row["Dependent"] as string, LogicalDiskRef);
                    if (letter is null || partitionId is null || !partitionToDisk.ContainsKey(partitionId))
                    {
                        continue;
                    }

                    if (!letterToPartitions.TryGetValue(letter, out List<string>? partitions))
                    {
                        partitions = new List<string>();
                        letterToPartitions[letter] = partitions;
                    }

                    partitions.Add(partitionId);
                }
            }
        }
        catch (Exception)
        {
            return new Dictionary<string, IReadOnlyList<int>>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, IReadOnlyList<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, List<string>> entry in letterToPartitions)
        {
            result[entry.Key] = entry.Value
                .Select(p => partitionToDisk[p])
                .Distinct()
                .OrderBy(i => i)
                .ToList();
        }

        return result;
    }

    private static string? ExtractKey(string? refString, Regex regex)
    {
        if (refString is null)
        {
            return null;
        }

        Match match = regex.Match(refString);
        return match.Success ? match.Groups[1].Value : null;
    }
}
