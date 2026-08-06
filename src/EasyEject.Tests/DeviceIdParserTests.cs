using EasyEject.Core.Services;
using Xunit;

namespace EasyEject.Tests;

/// <summary>
/// Tests for <see cref="DeviceIdParser"/>.
/// </summary>
public class DeviceIdParserTests
{
    [Theory]
    [InlineData(@"USBSTOR\DISK&VEN_KINGSTON&PROD_DATATRAVELER&REV_1.00\0000000000012", "KINGSTON", "DATATRAVELER")]
    [InlineData(@"USBSTOR\DISK&VEN_SAMSUNG&PROD_PSSD_T7&REV_0\abc123", "SAMSUNG", "PSSD_T7")]
    [InlineData(@"SCSI\DISK&VEN_WD&PRODUCT_MYPASSPORT_2620&REV_1016\xyz", "WD", "MYPASSPORT_2620")]
    [InlineData(@"USBSTOR\DISK&VEN_GENERIC&PRODUCT_MULTI_READER&REV_1.00\000000", "GENERIC", "MULTI_READER")]
    public void Parse_ExtractsVendorAndProduct(string instanceId, string vendor, string product)
    {
        (string? actualVendor, string? actualProduct) = DeviceIdParser.Parse(instanceId);
        Assert.Equal(vendor, actualVendor);
        Assert.Equal(product, actualProduct);
    }

    [Fact]
    public void Parse_IdWithoutSegments_ReturnsNulls()
    {
        (string? vendor, string? product) = DeviceIdParser.Parse("USBSTOR");
        Assert.Null(vendor);
        Assert.Null(product);
    }

    [Fact]
    public void Parse_IdWithoutVenProd_ReturnsNulls()
    {
        (string? vendor, string? product) = DeviceIdParser.Parse(@"IDE\DiskWDC_WD10JPVX\0123");
        Assert.Null(vendor);
        Assert.Null(product);
    }
}
