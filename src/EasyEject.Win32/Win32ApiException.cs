namespace EasyEject.Win32;

/// <summary>
/// Thrown when a Win32 call fails with an unexpected error.
/// </summary>
public sealed class Win32ApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ApiException"/> class.
    /// </summary>
    /// <param name="operation">The Win32 operation that failed.</param>
    /// <param name="errorCode">The Win32 error code (GetLastError value).</param>
    /// <param name="message">An optional custom message.</param>
    public Win32ApiException(string operation, int errorCode, string? message = null)
        : base($"{message ?? $"The Win32 operation '{operation}' failed"} (Win32 error {errorCode}: {GetMessage(errorCode)}).")
    {
        Operation = operation;
        ErrorCode = errorCode;
    }

    /// <summary>Gets the name of the failed Win32 operation.</summary>
    public string Operation { get; }

    /// <summary>Gets the Win32 error code.</summary>
    public int ErrorCode { get; }

    private static string GetMessage(int errorCode)
    {
        try
        {
            return new System.ComponentModel.Win32Exception(errorCode).Message;
        }
        catch
        {
            return $"0x{errorCode:X8}";
        }
    }
}
