using EasyEject.Core.Contracts;
using EasyEject.Models;

namespace EasyEject.Core.Services;

/// <summary>
/// Orchestrates ejecting multiple devices sequentially and aggregates the results.
/// </summary>
public sealed class EjectAllCoordinator
{
    private readonly IEjector _ejector;

    /// <summary>
    /// Initializes a new instance of the <see cref="EjectAllCoordinator"/> class.
    /// </summary>
    /// <param name="ejector">The ejector used for each device.</param>
    public EjectAllCoordinator(IEjector ejector)
    {
        _ejector = ejector ?? throw new ArgumentNullException(nameof(ejector));
    }

    /// <summary>
    /// Ejects a single device.
    /// </summary>
    /// <param name="device">The device to eject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The eject result.</returns>
    public async Task<EjectResult> EjectSingleAsync(ExternalDevice device, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _ejector.EjectAsync(device, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new EjectResult(device.DeviceId, device.FriendlyName, EjectStatus.Failed, ex.Message);
        }
    }

    /// <summary>
    /// Ejects every device, one at a time, reporting progress.
    /// Retries busy/vetoed devices once after a short delay.
    /// </summary>
    private const int SettleDelayMs = 300;
    private const int EjectRetryDelayMs = 500;
    private const int MaxEjectRetries = 1;

    public async Task<EjectAllResult> EjectAllAsync(
        IReadOnlyList<ExternalDevice> devices,
        IProgress<EjectProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EjectResult>(devices.Count);
        int completed = 0;
        int succeeded = 0;
        int failed = 0;

        for (int i = 0; i < devices.Count; i++)
        {
            ExternalDevice device = devices[i];
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new EjectProgress(completed, devices.Count, device.FriendlyName, null));

            EjectResult result = await EjectWithRetryAsync(device, cancellationToken).ConfigureAwait(false);

            results.Add(result);
            completed++;

            if (result.IsSuccess)
            {
                succeeded++;
            }
            else
            {
                failed++;
            }

            progress?.Report(new EjectProgress(completed, devices.Count, device.FriendlyName, result.Status));

            // Settle delay between devices so Windows can finish processing the previous eject.
            if (i < devices.Count - 1)
            {
                await Task.Delay(SettleDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        return new EjectAllResult(results, succeeded, failed);
    }

    private async Task<EjectResult> EjectWithRetryAsync(ExternalDevice device, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= MaxEjectRetries; attempt++)
        {
            EjectResult result;
            try
            {
                result = await _ejector.EjectAsync(device, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new EjectResult(device.DeviceId, device.FriendlyName, EjectStatus.Failed, ex.Message);
            }

            if (result.IsSuccess || result.Status == EjectStatus.AlreadyRemoved)
            {
                return result;
            }

            // Retry on busy/veto — wait for handles to close.
            if (attempt < MaxEjectRetries &&
                (result.Status == EjectStatus.DeviceBusy || result.Status == EjectStatus.Vetoed))
            {
                await Task.Delay(EjectRetryDelayMs, cancellationToken).ConfigureAwait(false);
                continue;
            }

            return result;
        }

        // Should not reach here, but fallback.
        return new EjectResult(device.DeviceId, device.FriendlyName, EjectStatus.Failed, "Exhausted retries.");
    }
}
