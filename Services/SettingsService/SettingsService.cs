using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinCarePro.Database;
using WinCarePro.Models;
using WinCarePro.Services.Contracts;

namespace WinCarePro.Services.Implementations;

public class SettingsService : ISettingsService, IDisposable
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private SettingsProfile _currentSettings = new();
    private readonly object _lock = new();
    private Timer? _debounceTimer;
    private readonly int _debounceMs = 500; // Increased from 300ms for better batching
    private long _settingsVersion = 0; // Thread-safe version counter for race prevention
    private bool _disposed;

    public SettingsProfile CurrentSettings
    {
        get
        {
            lock (_lock)
            {
                return _currentSettings;
            }
        }
    }

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public SettingsService()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        try
        {
            string raw = DbManager.GetSettings();
            if (!string.IsNullOrEmpty(raw))
            {
                var profile = JsonSerializer.Deserialize<SettingsProfile>(raw);
                if (profile != null)
                {
                    lock (_lock)
                    {
                        _currentSettings = profile;
                    }
                    SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(_currentSettings));
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
        }

        lock (_lock)
        {
            _currentSettings = new SettingsProfile();
        }
    }

    public void UpdateSettings(Action<SettingsProfile> updateAction, string? propertyName = null)
    {
        SettingsProfile snapshot;
        lock (_lock)
        {
            updateAction(_currentSettings);
            snapshot = CloneSettings(_currentSettings);
        }

        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(snapshot, propertyName));
        QueuePersistSettings(snapshot);
    }

    public async Task SaveSettingsAsync(SettingsProfile profile)
    {
        SettingsProfile snapshot;
        lock (_lock)
        {
            _currentSettings = CloneSettings(profile);
            snapshot = CloneSettings(_currentSettings);
        }

        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(snapshot));
        await Task.Run(() => PersistToDatabase(snapshot));
    }

    public void ResetToDefaults()
    {
        var defaultProfile = new SettingsProfile();
        lock (_lock)
        {
            _currentSettings = defaultProfile;
        }

        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(defaultProfile, "Reset"));
        QueuePersistSettings(defaultProfile);
    }

    public string ExportSettingsJson()
    {
        lock (_lock)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(_currentSettings, options);
        }
    }

    public bool ImportSettingsJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            var profile = JsonSerializer.Deserialize<SettingsProfile>(json);
            if (profile != null)
            {
                lock (_lock)
                {
                    _currentSettings = profile;
                }
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(CloneSettings(profile), "Import"));
                QueuePersistSettings(CloneSettings(profile));
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to import settings: {ex.Message}");
        }
        return false;
    }

    private void QueuePersistSettings(SettingsProfile snapshot)
    {
        // Increment version atomically — only the latest version will actually persist
        long version = Interlocked.Increment(ref _settingsVersion);

        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                // Only persist if no newer version has been queued since this timer was set
                if (Interlocked.Read(ref _settingsVersion) == version)
                {
                    PersistToDatabase(snapshot);
                }
            }, null, _debounceMs, Timeout.Infinite);
        }
    }

    private void PersistToDatabase(SettingsProfile profile)
    {
        try
        {
            string json = JsonSerializer.Serialize(profile);
            DbManager.SaveSettings(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to save settings to DB: {ex.Message}");
        }
    }

    private static SettingsProfile CloneSettings(SettingsProfile source)
    {
        try
        {
            string json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<SettingsProfile>(json) ?? new SettingsProfile();
        }
        catch
        {
            return new SettingsProfile();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }
}

