namespace EasyEject.Core.Services;

/// <summary>
/// Parses vendor and product names from Windows device instance IDs.
/// </summary>
public static class DeviceIdParser
{
    /// <summary>
    /// Parses a device instance ID such as
    /// <c>USBSTOR\DISK&amp;VEN_KINGSTON&amp;PROD_DATATRAVELER&amp;REV_1.00\…</c>.
    /// </summary>
    /// <param name="instanceId">The device instance ID.</param>
    /// <returns>The vendor and product name, when present.</returns>
    public static (string? Vendor, string? Product) Parse(string instanceId)
    {
        string? vendor = null;
        string? product = null;

        string[] segments = instanceId.Split('\\', 2);
        if (segments.Length < 2)
        {
            return (vendor, product);
        }

        foreach (string token in segments[1].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith("VEN_", StringComparison.OrdinalIgnoreCase))
            {
                vendor = token[4..];
            }
            else if (token.StartsWith("PROD_", StringComparison.OrdinalIgnoreCase))
            {
                product = token[5..];
            }
            else if (token.StartsWith("PRODUCT_", StringComparison.OrdinalIgnoreCase))
            {
                product = token[8..];
            }
        }

        return (string.IsNullOrWhiteSpace(vendor) ? null : vendor, string.IsNullOrWhiteSpace(product) ? null : product);
    }
}
