using EasyEject.Core.Services;
using EasyEject.Models;
using Xunit;

namespace EasyEject.Tests;

/// <summary>
/// Tests for <see cref="EjectMessages"/> and <see cref="EnumTexts"/>.
/// </summary>
public class MessagesTests
{
    [Fact]
    public void SummaryFor_AllSuccess_Singular()
    {
        var result = new EjectAllResult(
            [new EjectResult("a", "Stick", EjectStatus.Success)],
            SucceededCount: 1,
            FailedCount: 0);

        Assert.Equal("1 device successfully ejected.", EjectMessages.SummaryFor(result));
    }

    [Fact]
    public void SummaryFor_AllSuccess_Plural()
    {
        var result = new EjectAllResult(
            [new EjectResult("a", "A", EjectStatus.Success), new EjectResult("b", "B", EjectStatus.Success)],
            SucceededCount: 2,
            FailedCount: 0);

        Assert.Equal("2 devices successfully ejected.", EjectMessages.SummaryFor(result));
    }

    [Fact]
    public void SummaryFor_Mixed_ReportsBothCounts()
    {
        var result = new EjectAllResult(
            [new EjectResult("a", "A", EjectStatus.Success), new EjectResult("b", "B", EjectStatus.DeviceBusy)],
            SucceededCount: 1,
            FailedCount: 1);

        Assert.Equal("1 device ejected, 1 failed.", EjectMessages.SummaryFor(result));
    }

    [Fact]
    public void SummaryFor_AllFailed_ReportsNoSuccess()
    {
        var result = new EjectAllResult(
            [new EjectResult("b", "B", EjectStatus.AccessDenied)],
            SucceededCount: 0,
            FailedCount: 1);

        Assert.Equal("No devices were ejected (1 failed).", EjectMessages.SummaryFor(result));
    }

    [Fact]
    public void ExplanationFor_Busy_IsActionable()
    {
        string text = EjectMessages.ExplanationFor(EjectStatus.DeviceBusy, null);
        Assert.Contains("close any open files", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(BusType.Usb, "USB")]
    [InlineData(BusType.Nvme, "NVMe")]
    [InlineData(BusType.Sata, "SATA")]
    [InlineData(BusType.Sd, "SD")]
    public void EnumTexts_BusType_IsFriendly(BusType bus, string expected)
    {
        Assert.Equal(expected, EnumTexts.BusType(bus));
    }

    [Theory]
    [InlineData(DeviceType.UsbFlashDrive, "USB Flash Drive")]
    [InlineData(DeviceType.SdCard, "SD Card")]
    [InlineData(DeviceType.UsbSolidStateDrive, "External SSD")]
    public void EnumTexts_DeviceType_IsFriendly(DeviceType type, string expected)
    {
        Assert.Equal(expected, EnumTexts.DeviceType(type));
    }
}
