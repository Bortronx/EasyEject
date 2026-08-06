using EasyEject.Core.Contracts;
using EasyEject.Core.Services;
using EasyEject.Models;
using Xunit;

namespace EasyEject.Tests;

/// <summary>
/// Tests for <see cref="EjectAllCoordinator"/>.
/// </summary>
public class EjectAllCoordinatorTests
{
    private static ExternalDevice Device(string id, string name) => new()
    {
        DeviceId = id,
        FriendlyName = name,
        DiskNumber = 0,
        BusType = BusType.Usb,
        IsRemovable = true,
        CanEject = true,
        DeviceType = DeviceType.UsbFlashDrive,
        Icon = DeviceIcon.Usb,
    };

    private sealed class FakeEjector : IEjector
    {
        private readonly Dictionary<string, EjectResult> _results;

        public FakeEjector(Dictionary<string, EjectResult> results)
        {
            _results = results;
        }

        public Task<EjectResult> EjectAsync(ExternalDevice device, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_results.TryGetValue(device.DeviceId, out EjectResult? result)
                ? result
                : new EjectResult(device.DeviceId, device.FriendlyName, EjectStatus.Success));
        }
    }

    private sealed class ProgressCollector : IProgress<EjectProgress>
    {
        public List<EjectProgress> Items { get; } = new();

        public void Report(EjectProgress value) => Items.Add(value);
    }

    [Fact]
    public async Task EjectAll_AllSuccess_AggregatesSuccess()
    {
        var devices = new List<ExternalDevice> { Device("A", "Stick A"), Device("B", "Stick B"), Device("C", "Stick C") };
        var ejector = new FakeEjector(new Dictionary<string, EjectResult>
        {
            ["A"] = new("A", "Stick A", EjectStatus.Success),
            ["B"] = new("B", "Stick B", EjectStatus.Success),
            ["C"] = new("C", "Stick C", EjectStatus.Success),
        });

        var coordinator = new EjectAllCoordinator(ejector);
        EjectAllResult result = await coordinator.EjectAllAsync(devices);

        Assert.Equal(3, result.SucceededCount);
        Assert.Equal(0, result.FailedCount);
        Assert.All(result.Results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public async Task EjectAll_MixedResults_CountsFailuresAndPreservesMessages()
    {
        var devices = new List<ExternalDevice> { Device("A", "Stick A"), Device("B", "Stick B") };
        var ejector = new FakeEjector(new Dictionary<string, EjectResult>
        {
            ["A"] = new("A", "Stick A", EjectStatus.DeviceBusy, "The device is currently in use."),
            ["B"] = new("B", "Stick B", EjectStatus.Success),
        });

        var coordinator = new EjectAllCoordinator(ejector);
        EjectAllResult result = await coordinator.EjectAllAsync(devices);

        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(EjectStatus.DeviceBusy, result.Results[0].Status);
        Assert.Equal("The device is currently in use.", result.Results[0].Message);
    }

    [Fact]
    public async Task EjectAll_ReportsProgressBeforeAndAfterEachDevice()
    {
        var devices = new List<ExternalDevice> { Device("A", "Stick A"), Device("B", "Stick B") };
        var ejector = new FakeEjector(new Dictionary<string, EjectResult>
        {
            ["A"] = new("A", "Stick A", EjectStatus.Success),
            ["B"] = new("B", "Stick B", EjectStatus.Failed),
        });

        var progress = new ProgressCollector();
        var coordinator = new EjectAllCoordinator(ejector);
        await coordinator.EjectAllAsync(devices, progress);

        // Before A (0/2, "Stick A"), after A (1/2, Success), before B (1/2, "Stick B"), after B (2/2, Failed)
        Assert.Equal(4, progress.Items.Count);
        Assert.Equal((0, 2, "Stick A", (EjectStatus?)null), (progress.Items[0].Completed, progress.Items[0].Total, progress.Items[0].CurrentName, progress.Items[0].CurrentStatus));
        Assert.Equal(EjectStatus.Success, progress.Items[1].CurrentStatus);
        Assert.Equal(1, progress.Items[2].Completed);
        Assert.Equal("Stick B", progress.Items[2].CurrentName);
        Assert.Equal(EjectStatus.Failed, progress.Items[3].CurrentStatus);
        Assert.Equal(2, progress.Items[3].Completed);
    }

    [Fact]
    public async Task EjectAll_EjectorThrows_MarksDeviceAsFailed()
    {
        var devices = new List<ExternalDevice> { Device("A", "Stick A") };
        var throwing = new ThrowingEjector();

        var coordinator = new EjectAllCoordinator(throwing);
        EjectAllResult result = await coordinator.EjectAllAsync(devices);

        Assert.Equal(0, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(EjectStatus.Failed, result.Results[0].Status);
    }

    private sealed class ThrowingEjector : IEjector
    {
        public Task<EjectResult> EjectAsync(ExternalDevice device, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
