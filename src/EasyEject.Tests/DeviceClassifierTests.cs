using EasyEject.Core.Services;
using EasyEject.Models;
using Xunit;

namespace EasyEject.Tests;

/// <summary>
/// Tests for <see cref="DeviceClassifier"/>.
/// </summary>
public class DeviceClassifierTests
{
    [Theory]
    [InlineData(BusType.Usb, "DataTraveler 3.0", DeviceType.UsbFlashDrive)]
    [InlineData(BusType.Usb, "Samsung Portable SSD T7", DeviceType.UsbSolidStateDrive)]
    [InlineData(BusType.Usb, "WD My Passport SSD", DeviceType.UsbSolidStateDrive)]
    [InlineData(BusType.Usb, "SanDisk Extreme SSD 1TB", DeviceType.UsbSolidStateDrive)]
    [InlineData(BusType.Usb, "Samsung X5 NVMe", DeviceType.UsbNvme)]
    [InlineData(BusType.Usb, "USB 3.0 SATA Adapter", DeviceType.UsbSataAdapter)]
    [InlineData(BusType.Sd, "SD Card", DeviceType.SdCard)]
    [InlineData(BusType.Usb, "USB2.0 Card Reader", DeviceType.CardReader)]
    [InlineData(BusType.Sata, "Seagate Expansion HDD", DeviceType.UsbHardDrive)]
    public void Classify_ReturnsExpectedType(BusType bus, string product, DeviceType expected)
    {
        Assert.Equal(expected, DeviceClassifier.Classify(bus, product));
    }

    [Fact]
    public void Classify_SdCardByBus_EvenWithoutProductName()
    {
        Assert.Equal(DeviceType.SdCard, DeviceClassifier.Classify(BusType.Sd, null));
    }

    [Theory]
    [InlineData(false, BusType.Usb, true)]
    [InlineData(false, BusType.Sd, true)]
    [InlineData(false, BusType.IEEE1394, true)]
    [InlineData(true, BusType.Ata, true)]
    [InlineData(false, BusType.Ata, false)]
    [InlineData(false, BusType.Nvme, false)]
    public void CanEject_ReflectsRemovabilityAndBus(bool isRemovable, BusType bus, bool expected)
    {
        Assert.Equal(expected, DeviceClassifier.CanEject(isRemovable, bus));
    }

    [Fact]
    public void IconFor_UsbFlashDrive_IsUsb()
    {
        Assert.Equal(DeviceIcon.Usb, DeviceClassifier.IconFor(DeviceType.UsbFlashDrive));
    }

    [Fact]
    public void IconFor_SdCard_IsMemoryCard()
    {
        Assert.Equal(DeviceIcon.MemoryCard, DeviceClassifier.IconFor(DeviceType.SdCard));
    }

    [Fact]
    public void IconFor_ExternalHdd_IsHardDrive()
    {
        Assert.Equal(DeviceIcon.HardDrive, DeviceClassifier.IconFor(DeviceType.UsbHardDrive));
    }
}
