using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
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

    private string _severity = "Warning"; // Canonical: "Critical", "Warning", "Info"
    public string Severity
    {
        get => _severity;
        set
        {
            if (SetProperty(ref _severity, value))
            {
                OnPropertyChanged(nameof(SeverityText));
                OnPropertyChanged(nameof(SeverityBrush));
            }
        }
    }
    public string SeverityText => Severity.T();

    private string _status = "Pending"; // Canonical: "Pending", "Fixing", "Fixed", "Failed"
    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsNotFixed));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }
    }
    public string StatusText => Status.T();

    private bool _isSelected = true;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    public bool IsNotFixed => Status != "Fixed";

    public Brush SeverityBrush
    {
        get
        {
            if (string.IsNullOrEmpty(Severity)) return new SolidColorBrush(Microsoft.UI.Colors.Gray);
            string s = Severity.ToLowerInvariant();
            if (s.Contains("critical") || s.Contains("nguy hiểm") || s.Contains("nghiêm trọng")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
            if (s.Contains("warning") || s.Contains("cảnh báo")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)); // Amber
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 130, 246)); // Blue
        }
    }

    public Brush StatusBrush
    {
        get
        {
            if (string.IsNullOrEmpty(Status)) return new SolidColorBrush(Microsoft.UI.Colors.Gray);
            string s = Status.ToLowerInvariant();
            if (s.Contains("fixed") || s.Contains("đã sửa") || s.Contains("success") || s.Contains("thành công")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Green
            if (s.Contains("fixing") || s.Contains("đang sửa")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 92, 246)); // Purple
            if (s.Contains("fail") || s.Contains("thất bại") || s.Contains("lỗi")) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 107, 114, 128)); // Gray (Pending)
        }
    }
}

public class RepairViewModel : ViewModelBase
{
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly SystemEngine _repairEngine = App.Services?.GetService<SystemEngine>() ?? new();
    private CancellationTokenSource? _cts;

    private readonly StringBuilder _logBuffer = new();
    private bool _isLogUpdatePending;
    private readonly object _logLock = new();

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

    private string _currentScanStepText = "";
    public string CurrentScanStepText
    {
        get => _currentScanStepText;
        set => SetProperty(ref _currentScanStepText, value);
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

    private string _activeTab = "diagnostics";
    public string ActiveTab
    {
        get => _activeTab;
        set => SetProperty(ref _activeTab, value);
    }

    private string _selectedFilterCategory = "All";
    public string SelectedFilterCategory
    {
        get => _selectedFilterCategory;
        set
        {
            if (SetProperty(ref _selectedFilterCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    private bool _isConsoleVisible = true;
    public bool IsConsoleVisible
    {
        get => _isConsoleVisible;
        set => SetProperty(ref _isConsoleVisible, value);
    }

    private bool _isAutoScrollEnabled = true;
    public bool IsAutoScrollEnabled
    {
        get => _isAutoScrollEnabled;
        set => SetProperty(ref _isAutoScrollEnabled, value);
    }

    private int _logLineCount = 1;
    public int LogLineCount
    {
        get => _logLineCount;
        set => SetProperty(ref _logLineCount, value);
    }

    public ObservableCollection<RepairServiceItem> Services { get; } = new();
    public ObservableCollection<DiagnosticIssueItem> DiscoveredIssues { get; } = new();
    public ObservableCollection<DiagnosticIssueItem> FilteredDiscoveredIssues { get; } = new();

    public RepairViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? App.MainDispatcherQueue;

        _repairEngine.OutputReceived += LogText;
        if (_dispatcherQueue != null)
        {
            _repairEngine.ProgressChanged += Pct => _dispatcherQueue.TryEnqueue(() => RepairProgressPercent = Pct);
        }

        LoadServices();
    }

    public void CancelCurrentOperation()
    {
        // Capture volatile reference to prevent race condition with async finally blocks
        var cts = _cts;
        if (cts != null && !cts.IsCancellationRequested)
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
            LogText("Cancelling current operation by user request...".T());
            CurrentScanStepText = "Cancelling operation...".T();
        }
    }

    public void LoadServices()
    {
        var dispatcher = _dispatcherQueue ?? App.MainDispatcherQueue;
        if (dispatcher == null) return;

        Task.Run(() =>
        {
            var targetServices = new[]
            {
                new { Name = "wuauserv", Display = "Windows Update (wuauserv)" },
                new { Name = "bits", Display = "Background Intelligent Transfer (bits)" },
                new { Name = "cryptsvc", Display = "Cryptographic Services (cryptsvc)" },
                new { Name = "winmgmt", Display = "Windows Management Instrumentation (winmgmt)" },
                new { Name = "mpssvc", Display = "Windows Defender Firewall (mpssvc)" }
            };

            var tempServices = new List<RepairServiceItem>();
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
                catch { }

                if (isRunning) runningCount++;

                tempServices.Add(new RepairServiceItem
                {
                    Name = ts.Name,
                    DisplayName = ts.Display,
                    Status = status,
                    StartupType = startupType,
                    IsSelected = false
                });
            }

            dispatcher.TryEnqueue(() =>
            {
                Services.Clear();
                foreach (var item in tempServices)
                {
                    Services.Add(item);
                }
                ServicesHealthyCount = runningCount;
            });
        });
    }

    private bool _isAdmin = SystemEngine.IsUserAnAdmin();
    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetProperty(ref _isAdmin, value);
    }

    private void LogText(string msg)
    {
        lock (_logLock)
        {
            _logBuffer.AppendLine(msg);
            _logLineCount++;
            if (_isLogUpdatePending) return;
            _isLogUpdatePending = true;
        }

        _dispatcherQueue?.TryEnqueue(() =>
        {
            string textToAppend;
            int totalLines;
            lock (_logLock)
            {
                textToAppend = _logBuffer.ToString();
                _logBuffer.Clear();
                _isLogUpdatePending = false;
                totalLines = _logLineCount;
            }
            if (!string.IsNullOrEmpty(textToAppend))
            {
                string newLog = ConsoleLog + textToAppend;
                if (newLog.Length > 80000)
                {
                    newLog = newLog.Substring(newLog.Length - 50000);
                }
                ConsoleLog = newLog;
                LogLineCount = totalLines;
            }
        });
    }

    public void ClearConsoleLog()
    {
        ConsoleLog = "Windows Repair Center Console Ready.\n".T();
        LogLineCount = 1;
    }

    public void CopyConsoleLog()
    {
        try
        {
            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.SetText(ConsoleLog);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
            LogText("Console log copied to clipboard.".T());
        }
        catch { }
    }

    public void ToggleConsoleVisibility()
    {
        IsConsoleVisible = !IsConsoleVisible;
    }

    public void ToggleAutoScroll()
    {
        IsAutoScrollEnabled = !IsAutoScrollEnabled;
    }

    public void SelectAllIssues()
    {
        foreach (var item in DiscoveredIssues)
        {
            if (item.IsNotFixed) item.IsSelected = true;
        }
    }

    public void DeselectAllIssues()
    {
        foreach (var item in DiscoveredIssues)
        {
            item.IsSelected = false;
        }
    }

    public void ApplyFilter()
    {
        FilteredDiscoveredIssues.Clear();
        foreach (var item in DiscoveredIssues)
        {
            if (SelectedFilterCategory == "All" || SelectedFilterCategory.T() == "Tất cả")
            {
                FilteredDiscoveredIssues.Add(item);
            }
            else if (SelectedFilterCategory.Equals(item.Severity, StringComparison.OrdinalIgnoreCase))
            {
                FilteredDiscoveredIssues.Add(item);
            }
        }
    }

    public async Task RunDiagnosticsScanAsync()
    {
        if (IsBusy || IsScanningDiagnostics) return;
        IsScanningDiagnostics = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        CurrentScanStepText = "Starting System Diagnostics Scan...".T();
        LogText("Starting System Diagnostics Scan...".T());

        DiscoveredIssues.Clear();

        try
        {
            // Step 1: Scan Services
            CurrentScanStepText = "Scanning core system services...".T();
            LogText("Scanning core system services...".T());
            RepairProgressPercent = 25;

            var targetServices = new[]
            {
                new { Name = "wuauserv", Display = "Windows Update (wuauserv)", Critical = true },
                new { Name = "bits", Display = "Background Intelligent Transfer (bits)", Critical = false },
                new { Name = "cryptsvc", Display = "Cryptographic Services (cryptsvc)", Critical = true },
                new { Name = "winmgmt", Display = "Windows Management Instrumentation (winmgmt)", Critical = true },
                new { Name = "mpssvc", Display = "Windows Defender Firewall (mpssvc)", Critical = true }
            };

            var stoppedServices = await Task.Run(() =>
            {
                var list = new List<(string Name, string Display, bool Critical, string StartType, string Status)>();
                int healthyCount = 0;
                foreach (var ts in targetServices)
                {
                    if (token.IsCancellationRequested) break;

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

                    if (isRunning)
                    {
                        healthyCount++;
                    }
                    else
                    {
                        list.Add((ts.Name, ts.Display, ts.Critical, startType, status));
                    }
                }
                return (list, healthyCount);
            }, token);

            token.ThrowIfCancellationRequested();
            ServicesHealthyCount = stoppedServices.healthyCount;

            foreach (var svc in stoppedServices.list)
            {
                DiscoveredIssues.Add(new DiagnosticIssueItem
                {
                    Id = $"SVC_{svc.Name}",
                    Title = string.Format("Service '{0}' is stopped".T(), svc.Display),
                    Description = string.Format("Service configured as '{0}' but is currently '{1}'.".T(), svc.StartType.T(), svc.Status.T()),
                    Category = "Core Services".T(),
                    Severity = svc.Critical ? "Critical" : "Warning",
                    Status = "Pending",
                    IsSelected = true
                });
            }

            // Step 2: Scan Registry Policies
            CurrentScanStepText = "Checking registry policy restrictions...".T();
            LogText("Checking registry policy restrictions...".T());
            RepairProgressPercent = 60;
            await Task.Delay(300, token);

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

            var restrictions = await Task.Run(() =>
            {
                var list = new List<(string Id, string Title, string Description)>();
                Microsoft.Win32.RegistryKey[] rootKeys = { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine };
                foreach (var root in rootKeys)
                {
                    if (token.IsCancellationRequested) break;

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
                                        string userFriendlyName = val switch
                                        {
                                            "DisableTaskMgr" => "Task Manager Restriction".T(),
                                            "DisableRegistryTools" => "Registry Editor Restriction".T(),
                                            "DisableCMD" => "Command Prompt Restriction".T(),
                                            "NoRun" => "Run Dialog Restriction".T(),
                                            "NoControlPanel" => "Control Panel Restriction".T(),
                                            _ => val
                                        };
                                        list.Add((
                                            $"REG_{root.Name}_{val}",
                                            string.Format("System Policy: {0}".T(), userFriendlyName),
                                            string.Format("Utility is disabled by registry value: {0}\\{1}\\{2}".T(), root.Name, path, val)
                                        ));
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                return list;
            }, token);

            token.ThrowIfCancellationRequested();

            foreach (var restriction in restrictions)
            {
                DiscoveredIssues.Add(new DiagnosticIssueItem
                {
                    Id = restriction.Id,
                    Title = restriction.Title,
                    Description = restriction.Description,
                    Category = "System Policies".T(),
                    Severity = "Critical",
                    Status = "Pending",
                    IsSelected = true
                });
            }
            RegistryRestrictionsCount = restrictions.Count;

            // Step 3: Check network connectivity with strict 3s timeout
            CurrentScanStepText = "Measuring DNS resolve latency...".T();
            LogText("Measuring DNS resolve latency...".T());
            RepairProgressPercent = 85;

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var dnsTask = System.Net.Dns.GetHostEntryAsync("www.google.com");
                var timeoutTask = Task.Delay(3000, token);

                var completedTask = await Task.WhenAny(dnsTask, timeoutTask);
                watch.Stop();

                if (completedTask == dnsTask && !dnsTask.IsFaulted)
                {
                    long ping = watch.ElapsedMilliseconds;
                    NetworkPingStatus = $"{ping} ms";

                    if (ping > 250)
                    {
                        DiscoveredIssues.Add(new DiagnosticIssueItem
                        {
                            Id = "NET_LATENCY",
                            Title = "High DNS Latency".T(),
                            Description = string.Format("DNS query resolved in {0} ms. Clean DNS resolver configurations to optimize latency.".T(), ping),
                            Category = "Network Health".T(),
                            Severity = "Warning",
                            Status = "Pending",
                            IsSelected = true
                        });
                    }
                }
                else
                {
                    NetworkPingStatus = "Timeout".T();
                    DiscoveredIssues.Add(new DiagnosticIssueItem
                    {
                        Id = "NET_OFFLINE",
                        Title = "DNS Resolution Timed Out".T(),
                        Description = "Standard host resolution took longer than 3 seconds or network is unreachable.".T(),
                        Category = "Network Health".T(),
                        Severity = "Warning",
                        Status = "Pending",
                        IsSelected = true
                    });
                }
            }
            catch
            {
                NetworkPingStatus = "Offline".T();
                DiscoveredIssues.Add(new DiagnosticIssueItem
                {
                    Id = "NET_OFFLINE",
                    Title = "DNS Resolution Failed / Offline".T(),
                    Description = "Failed to resolve standard host names. DNS client configuration reset is recommended.".T(),
                    Category = "Network Health".T(),
                    Severity = "Critical",
                    Status = "Pending",
                    IsSelected = true
                });
            }

            // Always add a recommendation to run SFC
            DiscoveredIssues.Add(new DiagnosticIssueItem
            {
                Id = "SYS_SFC",
                Title = "System Integrity Check Recommendation".T(),
                Description = "Run a system file integrity check (SFC/DISM) regularly to protect against unexpected corrupted DLLs.".T(),
                Category = "System Integrity".T(),
                Severity = "Info",
                Status = "Pending",
                IsSelected = false
            });

            RepairProgressPercent = 100;
            HasDiagnosticsRun = true;
            CurrentScanStepText = "Diagnostics Scan completed successfully.".T();
            LogText("Diagnostics Scan completed successfully.".T());

            // Calculate health score
            int penalty = 0;
            foreach (var issue in DiscoveredIssues)
            {
                if (issue.Severity == "Critical") penalty += 20;
                else if (issue.Severity == "Warning") penalty += 10;
            }
            HealthScore = Math.Max(100 - penalty, 0);
            DiscoveredIssuesCount = DiscoveredIssues.Count;

            ApplyFilter();
            LoadServices();
        }
        catch (OperationCanceledException)
        {
            CurrentScanStepText = "Diagnostics Scan was cancelled.".T();
            LogText("Diagnostics Scan was cancelled.".T());
        }
        catch (Exception ex)
        {
            CurrentScanStepText = string.Format("Diagnostics Scan failed: {0}".T(), ex.Message);
            LogText(string.Format("Diagnostics Scan failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsScanningDiagnostics = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task FixSingleIssueAsync(DiagnosticIssueItem issue)
    {
        if (IsBusy || issue == null || issue.Status == "Fixed") return;
        IsBusy = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        issue.Status = "Fixing";
        CurrentScanStepText = string.Format("Fixing issue: {0}...".T(), issue.Title);
        LogText(string.Format("Fixing single issue: {0}...".T(), issue.Title));

        try
        {
            bool success = false;
            if (issue.Id.StartsWith("SVC_"))
            {
                string svcName = issue.Id.Substring(4);
                success = await _repairEngine.RepairServicesConfigAsync(new[] { svcName }, token);
            }
            else if (issue.Id.StartsWith("REG_"))
            {
                success = await _repairEngine.RepairRegistryPoliciesAsync(token);
            }
            else if (issue.Id == "NET_LATENCY" || issue.Id == "NET_OFFLINE")
            {
                success = await _repairEngine.RepairNetworkStackAsync(token);
            }
            else if (issue.Id == "SYS_SFC")
            {
                success = await _repairEngine.RunSfcScanAsync(true, token);
            }

            if (success)
            {
                issue.Status = "Fixed";
                LogText(string.Format("Successfully resolved: {0}".T(), issue.Title));
            }
            else
            {
                issue.Status = "Failed";
                LogText(string.Format("Failed to resolve: {0}".T(), issue.Title));
            }

            LoadServices();
            RecalculateScore();
        }
        catch (OperationCanceledException)
        {
            issue.Status = "Pending";
            LogText(string.Format("Fix for {0} was cancelled.", issue.Title));
        }
        catch (Exception ex)
        {
            issue.Status = "Failed";
            LogText(string.Format("Error repairing issue: {0}", ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task FixAllSelectedIssuesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        CurrentScanStepText = "Starting automated repair for selected issues...".T();
        LogText("Starting automated repair for selected issues...".T());

        try
        {
            var selectedIssues = DiscoveredIssues.Where(x => x.IsSelected && x.Status != "Fixed").ToList();

            if (selectedIssues.Count == 0)
            {
                LogText("No pending selected issues to fix.".T());
                CurrentScanStepText = "No pending selected issues to fix.".T();
                return;
            }

            int fixedCount = 0;
            int total = selectedIssues.Count;

            for (int i = 0; i < total; i++)
            {
                token.ThrowIfCancellationRequested();

                var issue = selectedIssues[i];
                issue.Status = "Fixing";
                CurrentScanStepText = string.Format("Fixing ({0}/{1}): {2}...".T(), i + 1, total, issue.Title);
                LogText(string.Format("Fixing: {0}...".T(), issue.Title));

                bool success = false;
                if (issue.Id.StartsWith("SVC_"))
                {
                    string svcName = issue.Id.Substring(4);
                    success = await _repairEngine.RepairServicesConfigAsync(new[] { svcName }, token);
                }
                else if (issue.Id.StartsWith("REG_"))
                {
                    success = await _repairEngine.RepairRegistryPoliciesAsync(token);
                }
                else if (issue.Id == "NET_LATENCY" || issue.Id == "NET_OFFLINE")
                {
                    success = await _repairEngine.RepairNetworkStackAsync(token);
                }
                else if (issue.Id == "SYS_SFC")
                {
                    success = await _repairEngine.RunSfcScanAsync(true, token);
                }

                if (success)
                {
                    issue.Status = "Fixed";
                    LogText(string.Format("Successfully resolved: {0}".T(), issue.Title));
                    fixedCount++;
                }
                else
                {
                    issue.Status = "Failed";
                    LogText(string.Format("Failed to resolve: {0}".T(), issue.Title));
                }

                RepairProgressPercent = (int)(100.0 * (i + 1) / total);
                await Task.Delay(200, token);
            }

            LoadServices();
            RecalculateScore();

            CurrentScanStepText = string.Format("Automated repair finished. Fixed {0} of {1} issues.".T(), fixedCount, total);
            LogText(string.Format("Automated repair finished. Fixed {0} of {1} issues.".T(), fixedCount, total));
        }
        catch (OperationCanceledException)
        {
            CurrentScanStepText = "Automated repair operation was cancelled.".T();
            LogText("Automated repair operation was cancelled.".T());
        }
        catch (Exception ex)
        {
            CurrentScanStepText = string.Format("Repair execution encountered an error: {0}".T(), ex.Message);
            LogText(string.Format("Repair execution encountered an error: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void RecalculateScore()
    {
        int penalty = 0;
        foreach (var issue in DiscoveredIssues)
        {
            if (issue.Status != "Fixed")
            {
                if (issue.Severity == "Critical") penalty += 20;
                else if (issue.Severity == "Warning") penalty += 10;
            }
        }
        HealthScore = Math.Max(100 - penalty, 0);
        DiscoveredIssuesCount = DiscoveredIssues.Count;
    }

    public async Task RunSfcScanAsync(bool repair)
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();

        try
        {
            CurrentScanStepText = repair ? "Running SFC Repair...".T() : "Running SFC Verification Scan...".T();
            LogText(CurrentScanStepText);
            await _repairEngine.RunSfcScanAsync(repair, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            LogText("SFC operation was cancelled by user.".T());
            CurrentScanStepText = "SFC operation cancelled.".T();
        }
        catch (Exception ex)
        {
            LogText(string.Format("SFC command execution failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task RunDismOperationAsync(string mode)
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();

        try
        {
            CurrentScanStepText = string.Format("Running DISM operation: {0}...".T(), mode);
            LogText(CurrentScanStepText);
            await _repairEngine.RunDismAsync(mode, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            LogText("DISM operation was cancelled by user.".T());
            CurrentScanStepText = "DISM operation cancelled.".T();
        }
        catch (Exception ex)
        {
            LogText(string.Format("DISM execution failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task RepairWindowsUpdateAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();

        try
        {
            CurrentScanStepText = "Resetting Windows Update components...".T();
            await _repairEngine.RepairWindowsUpdateAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            LogText(string.Format("Windows Update repair execution failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task RepairServicesConfigAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();

        try
        {
            CurrentScanStepText = "Restoring selected services configuration...".T();
            var selected = Services.Where(s => s.IsSelected).Select(s => s.Name).ToList();
            await _repairEngine.RepairServicesConfigAsync(selected, _cts.Token);
            LoadServices();
        }
        catch (Exception ex)
        {
            LogText(string.Format("Services restoration failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task CreateRestorePointAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();

        try
        {
            CurrentScanStepText = "Creating System Restore Point...".T();
            await _repairEngine.CreateRestorePointAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            LogText(string.Format("System Restore Point creation failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task RepairRegistryPoliciesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();

        try
        {
            CurrentScanStepText = "Repairing registry policy restrictions...".T();
            await _repairEngine.RepairRegistryPoliciesAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            LogText(string.Format("Registry Policies repair failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public async Task RepairNetworkStackAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        RepairProgressPercent = 0;
        _cts = new CancellationTokenSource();

        try
        {
            CurrentScanStepText = "Resetting network stack & Winsock...".T();
            await _repairEngine.RepairNetworkStackAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            LogText(string.Format("Network Stack repair failed: {0}".T(), ex.Message));
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
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

