namespace EasyEject.Core.Services;

/// <summary>
/// Formats byte counts into human readable capacity strings.
/// </summary>
public static class CapacityFormatter
{
    private const long KiB = 1024;
    private const long MiB = KiB * 1024;
    private const long GiB = MiB * 1024;
    private const long TiB = GiB * 1024;

    /// <summary>
    /// Formats a byte count using IEC units (e.g. "64.0 GB").
    /// </summary>
    /// <param name="bytes">The byte count.</param>
    /// <returns>The formatted string, or an em dash when unknown.</returns>
    public static string Format(ulong? bytes)
    {
        if (bytes is null or 0)
        {
            return "—";
        }

        double value = bytes.Value;
        string unit;

        if (value >= TiB)
        {
            value /= TiB;
            unit = "TB";
        }
        else if (value >= GiB)
        {
            value /= GiB;
            unit = "GB";
        }
        else if (value >= MiB)
        {
            value /= MiB;
            unit = "MB";
        }
        else
        {
            value /= KiB;
            unit = "KB";
        }

        return $"{value:0.0} {unit}";
    }
}
