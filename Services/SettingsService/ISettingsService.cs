using System;
using System.Threading.Tasks;
using WinCarePro.Models;

namespace WinCarePro.Services.Contracts;

public class SettingsChangedEventArgs : EventArgs
{
    public SettingsProfile Settings { get; }
    public string? PropertyName { get; }

    public SettingsChangedEventArgs(SettingsProfile settings, string? propertyName = null)
    {
        Settings = settings;
        PropertyName = propertyName;
    }
}

public interface ISettingsService
{
    /// <summary>
    /// Gets the current in-memory cached settings profile (O(1) fast access).
    /// </summary>
    SettingsProfile CurrentSettings { get; }

    /// <summary>
    /// Fired when any setting is updated.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Loads settings from database storage into memory cache.
    /// </summary>
    void LoadSettings();

    /// <summary>
    /// Updates settings using an action and immediately dispatches changes and queues background persistence.
    /// </summary>
    void UpdateSettings(Action<SettingsProfile> updateAction, string? propertyName = null);

    /// <summary>
    /// Saves the current profile to persistent storage asynchronously.
    /// </summary>
    Task SaveSettingsAsync(SettingsProfile profile);

    /// <summary>
    /// Resets all settings to system factory defaults.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Exports the current settings profile as a formatted JSON string.
    /// </summary>
    string ExportSettingsJson();

    /// <summary>
    /// Imports and applies a settings profile from a JSON string.
    /// </summary>
    bool ImportSettingsJson(string json);
}
