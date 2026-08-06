using System.Text.Json;
using System.Text.Json.Serialization;
using EasyEject.Core.Contracts;
using EasyEject.Models;

namespace EasyEject.Services;

/// <summary>
/// Loads and persists application settings as JSON in the per-user app data folder.
/// </summary>
public sealed class SettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="baseDirectory">The folder that holds the settings file (defaults to %LOCALAPPDATA%\EasyEject).</param>
    public SettingsService(string? baseDirectory = null)
    {
        _settingsPath = Path.Combine(
            baseDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyEject"),
            "settings.json");
    }

    /// <inheritdoc />
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return Task.FromResult(new AppSettings());
            }

            string json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);
            return Task.FromResult(settings ?? new AppSettings());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            return Task.FromResult(new AppSettings());
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, Options);
            string tempPath = _settingsPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
