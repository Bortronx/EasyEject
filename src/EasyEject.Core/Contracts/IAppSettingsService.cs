using EasyEject.Models;

namespace EasyEject.Core.Contracts;

/// <summary>
/// Loads and persists application settings.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Loads the persisted settings (or defaults when nothing is stored).
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current settings.
    /// </summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
