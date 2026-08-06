using EasyEject.Models;

namespace EasyEject.Core.Services;

/// <summary>
/// Provides human friendly explanations for eject results and status values.
/// </summary>
public static class EjectMessages
{
    /// <summary>
    /// Returns a short title for a result status.
    /// </summary>
    public static string TitleFor(EjectStatus status)
    {
        return status switch
        {
            EjectStatus.Success => "Ejected successfully",
            EjectStatus.AlreadyRemoved => "Already removed",
            EjectStatus.DeviceBusy => "Device is busy",
            EjectStatus.DeviceInUse => "Device is in use",
            EjectStatus.AccessDenied => "Access denied",
            EjectStatus.NotRemovable => "Cannot eject",
            EjectStatus.Vetoed => "Eject refused",
            _ => "Eject failed",
        };
    }

    /// <summary>
    /// Returns a user-friendly explanation for a failed result.
    /// </summary>
    public static string ExplanationFor(EjectStatus status, string? detail)
    {
        return status switch
        {
            EjectStatus.DeviceBusy => "The device is currently in use. Close any open files or applications and try again.",
            EjectStatus.DeviceInUse => "Windows reports that a program is using this device. Close all files and try again.",
            EjectStatus.AccessDenied => "Access was denied. Run EasyEject with the same privileges as the user who is using the device, or close locking applications.",
            EjectStatus.NotRemovable => "This device does not support safe removal and was left untouched.",
            EjectStatus.Vetoed => string.IsNullOrWhiteSpace(detail) ? "Windows refused the eject request." : $"Windows refused the eject request ({detail}).",
            EjectStatus.Failed => string.IsNullOrWhiteSpace(detail) ? "The eject request failed. Try again." : detail,
            _ => "The eject request failed. Try again.",
        };
    }

    /// <summary>
    /// Builds the final summary line for an eject-all operation.
    /// </summary>
    public static string SummaryFor(EjectAllResult result)
    {
        if (result.SucceededCount == 0)
        {
            return result.FailedCount == 0
                ? "No devices to eject."
                : $"No devices were ejected ({result.FailedCount} failed).";
        }

        return result.FailedCount == 0
            ? $"{result.SucceededCount} device{(result.SucceededCount == 1 ? string.Empty : "s")} successfully ejected."
            : $"{result.SucceededCount} device{(result.SucceededCount == 1 ? string.Empty : "s")} ejected, {result.FailedCount} failed.";
    }
}
