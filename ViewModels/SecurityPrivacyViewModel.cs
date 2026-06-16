using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using WinCarePro.Engines;

namespace WinCarePro.ViewModels;

public class SecurityPrivacyViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SecurityPrivacyEngine _engine = new();

    private string _antivirusStatus = "Loading...";
    private string _firewallStatus = "Loading...";
    private string _bitLockerStatus = "Loading...";
    private string _statusMessage = "Ready";

    private bool _advertisingIdEnabled;
    private bool _telemetryEnabled;
    private bool _clipboardHistoryEnabled;
    private bool _restrictTrackingEnabled;
    private bool _isBusy;

    public string AntivirusStatus
    {
        get => _antivirusStatus;
        set => SetProperty(ref _antivirusStatus, value);
    }

    public string FirewallStatus
    {
        get => _firewallStatus;
        set => SetProperty(ref _firewallStatus, value);
    }

    public string BitLockerStatus
    {
        get => _bitLockerStatus;
        set => SetProperty(ref _bitLockerStatus, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool AdvertisingIdEnabled
    {
        get => _advertisingIdEnabled;
        set
        {
            if (SetProperty(ref _advertisingIdEnabled, value))
            {
                ApplyPrivacyChange("advertisingid", value);
            }
        }
    }

    public bool TelemetryEnabled
    {
        get => _telemetryEnabled;
        set
        {
            if (SetProperty(ref _telemetryEnabled, value))
            {
                ApplyPrivacyChange("telemetry", value);
            }
        }
    }

    public bool ClipboardHistoryEnabled
    {
        get => _clipboardHistoryEnabled;
        set
        {
            if (SetProperty(ref _clipboardHistoryEnabled, value))
            {
                ApplyPrivacyChange("clipboardhistory", value);
            }
        }
    }

    public bool RestrictTrackingEnabled
    {
        get => _restrictTrackingEnabled;
        set
        {
            if (SetProperty(ref _restrictTrackingEnabled, value))
            {
                ApplyPrivacyChange("tracking", value);
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ObservableCollection<string> AuditWarnings { get; } = new();

    public SecurityPrivacyViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _ = LoadSecurityAndPrivacyStateAsync();
    }

    public async Task LoadSecurityAndPrivacyStateAsync()
    {
        IsBusy = true;
        StatusMessage = "Auditing security layers and privacy registry flags...";
        AuditWarnings.Clear();

        try
        {
            AntivirusStatus = await Task.Run(() => _engine.GetAntivirusStatus());
            bool firewall = await Task.Run(() => _engine.GetFirewallStatus());
            FirewallStatus = firewall ? "Enabled (All Profiles)" : "Disabled / Weak";
            BitLockerStatus = await Task.Run(() => _engine.GetBitLockerStatus());

            // Load privacy toggles
            _advertisingIdEnabled = await Task.Run(() => _engine.GetPrivacySetting("advertisingid"));
            _telemetryEnabled = await Task.Run(() => _engine.GetPrivacySetting("telemetry"));
            _clipboardHistoryEnabled = await Task.Run(() => _engine.GetPrivacySetting("clipboardhistory"));
            _restrictTrackingEnabled = await Task.Run(() => _engine.GetPrivacySetting("tracking"));

            OnPropertyChanged(nameof(AdvertisingIdEnabled));
            OnPropertyChanged(nameof(TelemetryEnabled));
            OnPropertyChanged(nameof(ClipboardHistoryEnabled));
            OnPropertyChanged(nameof(RestrictTrackingEnabled));

            // Load audit audits
            var issues = await Task.Run(() => _engine.RunSecurityAudits());
            foreach (var issue in issues)
            {
                AuditWarnings.Add(issue);
            }

            StatusMessage = "Security & Privacy audit complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Audit failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPrivacyChange(string settingKey, bool enabled)
    {
        Task.Run(() =>
        {
            bool ok = _engine.SetPrivacySetting(settingKey, enabled);
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = ok ? $"Successfully configured {settingKey} privacy toggle." : $"Failed to modify {settingKey} permission (Admin rights required).";
            });
        });
    }

    public void ClearClipboard()
    {
        _engine.ClearClipboard();
        StatusMessage = "Clipboard history purged.";
    }

    public void ClearRecentFiles()
    {
        _engine.ClearRecentFiles();
        StatusMessage = "Recent items and Explorer Run history cleaned.";
    }
}
