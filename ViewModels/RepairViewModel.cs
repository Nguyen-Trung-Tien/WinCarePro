using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Engines;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public class DiagnosticIssueItem : ViewModelBase
{
    private string _id = "";
    public string Id { get => _id; set => SetProperty(ref _id, value); }

    private string _title = "";
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    private string _description = "";
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    private string _category = "";
    public string Category { get => _category; set => SetProperty(ref _category, value); }

    private string _severity = ""; // "Critical", "Warning", "Info"
    public string Severity { get => _severity; set => SetProperty(ref _severity, value); }

    private string _status = "Pending"; // "Pending", "Fixing", "Fixed", "Failed"
    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsNotFixed));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }
    }

    private bool _isSelected = true;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    public bool IsNotFixed => Status != "Fixed" && Status != "Fixed".T();

    public Brush SeverityBrush
    {
        get
        {
            if (string.IsNullOrEmpty(Severity)) return new SolidColorBrush(Microsoft.UI.Colors.Gray);
            string s = Severity.ToLower();
            if (s.Contains("critical") || s.Contains("nguy cấp")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
            if (s.Contains("warning") || s.Contains("cảnh báo")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)); // Blue
        }
    }

    public Brush StatusBrush
    {
        get
        {
            if (string.IsNullOrEmpty(Status)) return new SolidColorBrush(Microsoft.UI.Colors.Gray);
            string s = Status.ToLower();
            if (s.Contains("fixed") || s.Contains("success") || s.Contains("đã sửa")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green
            if (s.Contains("fixing") || s.Contains("đang sửa")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)); // Purple
            if (s.Contains("fail") || s.Contains("lỗi")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 107, 114, 128)); // Gray (Pending)
        }
    }
}

public class RepairViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SystemEngine _repairEngine = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private bool _isScanningDiagnostics;
    public bool IsScanningDiagnostics
    {
        get => _isScanningDiagnostics;
        set => SetProperty(ref _isScanningDiagnostics, value);
    }

    private bool _hasDiagnosticsRun;
    public bool HasDiagnosticsRun
    {
        get => _hasDiagnosticsRun;
        set => SetProperty(ref _hasDiagnosticsRun, value);
    }

    private int _discoveredIssuesCount;
    public int DiscoveredIssuesCount
    {
        get => _discoveredIssuesCount;
        set => SetProperty(ref _discoveredIssuesCount, value);
    }

    private int _servicesHealthyCount;
    public int ServicesHealthyCount
    {
        get => _servicesHealthyCount;
        set => SetProperty(ref _servicesHealthyCount, value);
    }

    private int _registryRestrictionsCount;
    public int RegistryRestrictionsCount
    {
        get => _registryRestrictionsCount;
        set => SetProperty(ref _registryRestrictionsCount, value);
    }

    private string _networkPingStatus = "N/A";
    public string NetworkPingStatus
    {
        get => _networkPingStatus;
        set => SetProperty(ref _networkPingStatus, value);
    }

    private int _healthScore = 100;
    public int HealthScore
    {
        get => _healthScore;
        set => SetProperty(ref _healthScore, value);
    }

    private string _consoleLog = "Windows Repair Center Console Ready.\n".T();
    public string ConsoleLog
    {
        get => _consoleLog;
        set => SetProperty(ref _consoleLog, value);
    }

    private int _repairProgressPercent;
    public int RepairProgressPercent
    {
        get => _repairProgressPercent;
        set => SetProperty(ref _repairProgressPercent, value);
    }

    public ObservableCollection<RepairServiceItem> Services { get; } = new();
    public ObservableCollection<DiagnosticIssueItem> DiscoveredIssues { get; } = new();

    public RepairViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _repairEngine.OutputReceived += LogText;
        _repairEngine.ProgressChanged += Pct => _dispatcherQueue.TryEnqueue(() => RepairProgressPercent = Pct);

        LoadServices();
    }

    public void LoadServices()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Services.Clear();
            var targetServices = new[]
            {
                new { Name = "wuauserv", Display = "Windows Update (wuauserv)" },
                new { Name = "bits", Display = "Background Intelligent Transfer (bits)" },
                new { Name = "cryptsvc", Display = "Cryptographic Services (cryptsvc)" },
                new { Name = "winmgmt", Display = "Windows Management Instrumentation (winmgmt)" },
                new { Name = "mpssvc", Display = "Windows Defender Firewall (mpssvc)" }
            };

            int runningCount = 0;
            foreach (var ts in targetServices)
            {
                string status = "Not Found".T();
                string startupType = "Unknown".T();
                bool isRunning = false;
                try
                {
                    using var svc = new System.ServiceProcess.ServiceController(ts.Name);
                    status = svc.Status.ToString().T();
                    startupType = svc.StartType.ToString().T();
                    isRunning = svc.Status == System.ServiceProcess.ServiceControllerStatus.Running;
                }
                catch {}

                if (isRunning) runningCount++;

                Services.Add(new RepairServiceItem
                {
                    Name = ts.Name,
                    DisplayName = ts.Display,
                    Status = status,
                    StartupType = startupType,
                    IsSelected = false
                });
            }
            ServicesHealthyCount = runningCount;
        });
    }

    private void LogText(string msg)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ConsoleLog += msg + "\n";
        });
    }

    public async Task RunDiagnosticsScanAsync()
    {
        if (IsBusy || IsScanningDiagnostics) return;
        IsScanningDiagnostics = true;
        RepairProgressPercent = 0;
        LogText("Starting System Diagnostics Scan...".T());

        _dispatcherQueue.TryEnqueue(() => DiscoveredIssues.Clear());

        try
        {
            // Step 1: Scan Services
            LogText("Scanning core system services...".T());
            RepairProgressPercent = 25;
            LoadServices();
            await Task.Delay(400);

            var targetServices = new[]
            {
                new { Name = "wuauserv", Display = "Windows Update (wuauserv)", Critical = true },
                new { Name = "bits", Display = "Background Intelligent Transfer (bits)", Critical = false },
                new { Name = "cryptsvc", Display = "Cryptographic Services (cryptsvc)", Critical = true },
                new { Name = "winmgmt", Display = "Windows Management Instrumentation (winmgmt)", Critical = true },
                new { Name = "mpssvc", Display = "Windows Defender Firewall (mpssvc)", Critical = true }
            };

            foreach (var ts in targetServices)
            {
                string status = "Not Found";
                string startType = "Unknown";
                bool isRunning = false;
                try
                {
                    using var svc = new System.ServiceProcess.ServiceController(ts.Name);
                    status = svc.Status.ToString();
                    startType = svc.StartType.ToString();
                    isRunning = svc.Status == System.ServiceProcess.ServiceControllerStatus.Running;
                }
                catch { }

                if (!isRunning)
                {
                    _dispatcherQueue.TryEnqueue(() => DiscoveredIssues.Add(new DiagnosticIssueItem
                    {
                        Id = $"SVC_{ts.Name}",
                        Title = string.Format("Service '{0}' is stopped".T(), ts.Display),
                        Description = string.Format("Service configured as '{0}' but is currently '{1}'.".T(), startType, status),
                        Category = "Core Services".T(),
                        Severity = ts.Critical ? "Critical".T() : "Warning".T(),
                        Status = "Pending".T(),
                        IsSelected = true
                    }));
                }
            }

            // Step 2: Scan Registry Policies
            LogText("Checking registry policy restrictions...".T());
            RepairProgressPercent = 60;
            await Task.Delay(400);
            int foundRestrictions = 0;

            string[] policyKeys = {
                @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
                @"Software\Policies\Microsoft\Windows\System",
                @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"
            };

            string[] policyValues = {
                "DisableTaskMgr",
                "DisableRegistryTools",
                "DisableCMD",
                "NoRun",
                "NoControlPanel"
            };

            Microsoft.Win32.RegistryKey[] rootKeys = { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine };
            foreach (var root in rootKeys)
            {
                foreach (var path in policyKeys)
                {
                    try
                    {
                        using var key = root.OpenSubKey(path, false);
                        if (key != null)
                        {
                            foreach (var val in policyValues)
                            {
                                if (key.GetValue(val) != null)
                                {
                                    foundRestrictions++;
                                    string userFriendlyName = val switch
                                    {
                                        "DisableTaskMgr" => "Task Manager Restriction".T(),
                                        "DisableRegistryTools" => "Registry Editor Restriction".T(),
                                        "DisableCMD" => "Command Prompt Restriction".T(),
                                        "NoRun" => "Run Dialog Restriction".T(),
                                        "NoControlPanel" => "Control Panel Restriction".T(),
                                        _ => val
                                    };
                                    _dispatcherQueue.TryEnqueue(() => DiscoveredIssues.Add(new DiagnosticIssueItem
                                    {
                                        Id = $"REG_{root.Name}_{val}",
                                        Title = string.Format("System Policy: {0}".T(), userFriendlyName),
                                        Description = string.Format("Utility is disabled by registry value: {0}\\{1}\\{2}".T(), root.Name, path, val),
                                        Category = "System Policies".T(),
                                        Severity = "Critical".T(),
                                        Status = "Pending".T(),
                                        IsSelected = true
                                    }));
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            RegistryRestrictionsCount = foundRestrictions;

            // Step 3: Check network connectivity
            LogText("Measuring DNS resolve latency...".T());
            RepairProgressPercent = 85;
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var host = await System.Net.Dns.GetHostEntryAsync("www.google.com");
                watch.Stop();
                long ping = watch.ElapsedMilliseconds;
                NetworkPingStatus = $"{ping} ms";

                if (ping > 250)
                {
                    _dispatcherQueue.TryEnqueue(() => DiscoveredIssues.Add(new DiagnosticIssueItem
                    {
                        Id = "NET_LATENCY",
                        Title = "High DNS Latency".T(),
                        Description = string.Format("DNS query resolved in {0} ms. Clean DNS resolver configurations to optimize latency.".T(), ping),
                        Category = "Network Health".T(),
                        Severity = "Warning".T(),
                        Status = "Pending".T(),
                        IsSelected = true
                    }));
                }
            }
            catch
            {
                NetworkPingStatus = "Offline".T();
                _dispatcherQueue.TryEnqueue(() => DiscoveredIssues.Add(new DiagnosticIssueItem
                {
                    Id = "NET_OFFLINE",
                    Title = "DNS Resolution Failed / Offline".T(),
                    Description = "Failed to resolve standard host names. DNS client configuration reset is recommended.".T(),
                    Category = "Network Health".T(),
                    Severity = "Critical".T(),
                    Status = "Pending".T(),
                    IsSelected = true
                }));
            }

            // Always add a recommendation to run SFC
            _dispatcherQueue.TryEnqueue(() => DiscoveredIssues.Add(new DiagnosticIssueItem
            {
                Id = "SYS_SFC",
                Title = "System Integrity Check Recommendation".T(),
                Description = "Run a system file integrity check (SFC/DISM) regularly to protect against unexpected corrupted DLLs.".T(),
                Category = "System Integrity".T(),
                Severity = "Info".T(),
                Status = "Pending".T(),
                IsSelected = false
            }));

            RepairProgressPercent = 100;
            HasDiagnosticsRun = true;
            LogText("Diagnostics Scan completed successfully.".T());

            // Calculate simple health score
            int penalty = 0;
            foreach (var issue in DiscoveredIssues)
            {
                if (issue.Severity == "Critical".T()) penalty += 20;
                else if (issue.Severity == "Warning".T()) penalty += 10;
            }
            HealthScore = Math.Max(100 - penalty, 0);
            DiscoveredIssuesCount = DiscoveredIssues.Count;
        }
        catch (Exception ex)
        {
            LogText(string.Format("Diagnostics Scan failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsScanningDiagnostics = false;
        }
    }

    public async Task FixAllSelectedIssuesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;

        LogText("Starting automated repair for selected issues...".T());

        try
        {
            var selectedIssues = new List<DiagnosticIssueItem>();
            foreach (var issue in DiscoveredIssues)
            {
                if (issue.IsSelected && issue.Status != "Fixed".T())
                {
                    selectedIssues.Add(issue);
                }
            }

            if (selectedIssues.Count == 0)
            {
                LogText("No pending selected issues to fix.".T());
                return;
            }

            int fixedCount = 0;
            int total = selectedIssues.Count;

            for (int i = 0; i < total; i++)
            {
                var issue = selectedIssues[i];
                issue.Status = "Fixing".T();
                LogText(string.Format("Fixing: {0}...".T(), issue.Title));

                bool success = false;
                if (issue.Id.StartsWith("SVC_"))
                {
                    string svcName = issue.Id.Substring(4);
                    success = await _repairEngine.RepairServicesConfigAsync(new[] { svcName });
                }
                else if (issue.Id.StartsWith("REG_"))
                {
                    success = await _repairEngine.RepairRegistryPoliciesAsync();
                }
                else if (issue.Id == "NET_LATENCY" || issue.Id == "NET_OFFLINE")
                {
                    success = await _repairEngine.RepairNetworkStackAsync();
                }
                else if (issue.Id == "SYS_SFC")
                {
                    success = await _repairEngine.RunSfcScanAsync(true);
                }

                if (success)
                {
                    issue.Status = "Fixed".T();
                    LogText(string.Format("Successfully resolved: {0}".T(), issue.Title));
                    fixedCount++;
                }
                else
                {
                    issue.Status = "Failed".T();
                    LogText(string.Format("Failed to resolve: {0}".T(), issue.Title));
                }

                RepairProgressPercent = (int)(100.0 * (i + 1) / total);
                await Task.Delay(300);
            }

            LoadServices();

            // Recalculate health score
            int penalty = 0;
            foreach (var issue in DiscoveredIssues)
            {
                if (issue.Status != "Fixed".T())
                {
                    if (issue.Severity == "Critical".T()) penalty += 20;
                    else if (issue.Severity == "Warning".T()) penalty += 10;
                }
            }
            HealthScore = Math.Max(100 - penalty, 0);
            DiscoveredIssuesCount = DiscoveredIssues.Count;

            LogText(string.Format("Automated repair finished. Fixed {0} of {1} issues.".T(), fixedCount, total));
        }
        catch (Exception ex)
        {
            LogText(string.Format("Repair execution encountered an error: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunSfcScanAsync(bool repair)
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;

        try
        {
            await _repairEngine.RunSfcScanAsync(repair);
        }
        catch (Exception ex)
        {
            LogText(string.Format("SFC command execution failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RunDismOperationAsync(string mode)
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;

        try
        {
            await _repairEngine.RunDismAsync(mode);
        }
        catch (Exception ex)
        {
            LogText(string.Format("DISM execution failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairWindowsUpdateAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;

        try
        {
            await _repairEngine.RepairWindowsUpdateAsync();
        }
        catch (Exception ex)
        {
            LogText(string.Format("Windows Update repair execution failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairServicesConfigAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;

        try
        {
            var selected = new List<string>();
            foreach (var s in Services)
            {
                if (s.IsSelected)
                {
                    selected.Add(s.Name);
                }
            }
            await _repairEngine.RepairServicesConfigAsync(selected);
            LoadServices();
        }
        catch (Exception ex)
        {
            LogText(string.Format("Services restoration failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CreateRestorePointAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        try
        {
            await _repairEngine.CreateRestorePointAsync();
        }
        catch (Exception ex)
        {
            LogText(string.Format("System Restore Point creation failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairRegistryPoliciesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        try
        {
            await _repairEngine.RepairRegistryPoliciesAsync();
        }
        catch (Exception ex)
        {
            LogText(string.Format("Registry Policies repair failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RepairNetworkStackAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        try
        {
            await _repairEngine.RepairNetworkStackAsync();
        }
        catch (Exception ex)
        {
            LogText(string.Format("Network Stack repair failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class RepairServiceItem : ViewModelBase
{
    private string _name = "";
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _displayName = "";
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    private string _status = "";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private string _startupType = "";
    public string StartupType
    {
        get => _startupType;
        set => SetProperty(ref _startupType, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
