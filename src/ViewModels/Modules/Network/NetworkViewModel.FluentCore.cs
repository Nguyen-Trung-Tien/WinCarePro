using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinCarePro.Infrastructure.Logging;
using WinCarePro.Models;
using WinCarePro.Services;

namespace WinCarePro.ViewModels;

public partial class NetworkViewModel
{
    // ==================== 1. ACTIVE NETWORK ADAPTER PROPERTIES ====================
    [ObservableProperty]
    private string _activeAdapterName = "--";

    [ObservableProperty]
    private string _ipv4Address = "--";

    [ObservableProperty]
    private string _subnetMask = "--";

    [ObservableProperty]
    private string _currentDnsDisplay = "--";

    [ObservableProperty]
    private string _adapterType = "--";

    [ObservableProperty]
    private string _macAddress = "--";

    // ==================== 2. ON-DEMAND DIAGNOSTICS PROPERTIES ====================
    [ObservableProperty]
    private bool _isDiagnosing;

    [ObservableProperty]
    private string _gatewayLatencyText = "--";

    [ObservableProperty]
    private string _cloudflareLatencyText = "--";

    [ObservableProperty]
    private string _googleLatencyText = "--";

    [ObservableProperty]
    private string _diagnosticsStatusText = "Sẵn sàng kiểm tra".T();

    [ObservableProperty]
    private bool _isInternetAvailable = true;

    // ==================== 3. SECURE DNS SWITCHER PROPERTIES ====================
    public ObservableCollection<DnsProfileOption> DnsProfiles { get; } = new();

    [ObservableProperty]
    private DnsProfileOption? _selectedDnsProfile;

    [ObservableProperty]
    private bool _isApplyingDns;

    [ObservableProperty]
    private bool _hasDnsBackup;

    [ObservableProperty]
    private string _dnsStatusMessage = "";

    // ==================== 4. RESCUE & REPAIR PROPERTIES ====================
    [ObservableProperty]
    private bool _isRepairing;

    [ObservableProperty]
    private bool _requiresRestart;

    [ObservableProperty]
    private string _repairStatusMessage = "";

    [ObservableProperty]
    private string _lastOperationResult = "";

    // ==================== INITIALIZATION & REFRESH ====================
    public void InitializeFluentCore()
    {
        LoadFluentDnsProfiles();
        RefreshActiveAdapter();
    }

    public void LoadFluentDnsProfiles()
    {
        DnsProfiles.Clear();
        foreach (var profile in _engine.GetPresetDnsProfiles())
        {
            DnsProfiles.Add(profile);
        }
        SelectedDnsProfile = DnsProfiles.FirstOrDefault();
    }

    [RelayCommand]
    public void RefreshActiveAdapter()
    {
        try
        {
            var summary = _engine.GetActiveAdapterSummary();
            RunOnUI(() =>
            {
                if (summary != null)
                {
                    ActiveAdapterName = summary.Name;
                    Ipv4Address = summary.IpAddresses;
                    SubnetMask = summary.SubnetMask;
                    GatewayAddress = summary.GatewayAddress;
                    CurrentDnsDisplay = summary.CurrentDnsServers;
                    CurrentDnsText = summary.CurrentDnsServers;
                    AdapterType = summary.Type;
                    MacAddress = summary.MacAddress;
                }
                else
                {
                    ActiveAdapterName = "No Active Adapter".T();
                    Ipv4Address = "--";
                    SubnetMask = "--";
                    GatewayAddress = "--";
                    CurrentDnsDisplay = "--";
                    CurrentDnsText = "--";
                    AdapterType = "--";
                    MacAddress = "--";
                }
                HasDnsBackup = _engine.HasDnsBackup;
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.RefreshActiveAdapter", ex);
        }
    }

    // ==================== ON-DEMAND DIAGNOSTICS COMMAND ====================
    [RelayCommand]
    public async Task RunOnDemandDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        if (IsDiagnosing || IsBusy) return;

        IsDiagnosing = true;
        DiagnosticsStatusText = "Đang kiểm tra kết nối...".T();

        try
        {
            var res = await _engine.RunOnDemandDiagnosticsAsync(cancellationToken);
            RunOnUI(() =>
            {
                GatewayLatencyText = res.GatewayLatencyText;
                CloudflareLatencyText = res.CloudflareLatencyText;
                GoogleLatencyText = res.GoogleLatencyText;
                IsInternetAvailable = res.IsInternetAvailable;
                DiagnosticsStatusText = res.IsInternetAvailable
                    ? "Kết nối internet ổn định.".T()
                    : "Mất kết nối hoặc độ trễ cao.".T();
            });
            RefreshActiveAdapter();
        }
        catch (OperationCanceledException)
        {
            DiagnosticsStatusText = "Đã dừng chẩn đoán.".T();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.RunOnDemandDiagnosticsAsync", ex);
            DiagnosticsStatusText = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsDiagnosing = false;
        }
    }

    // ==================== DNS SWITCHER COMMANDS ====================
    [RelayCommand]
    public async Task ApplySelectedDnsAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDnsProfile == null || IsApplyingDns || IsBusy) return;

        IsApplyingDns = true;
        DnsStatusMessage = string.Format("Đang áp dụng {0}...".T(), SelectedDnsProfile.Name);

        try
        {
            bool ok = await _engine.ApplyDnsProfileAsync(
                SelectedDnsProfile.Name,
                SelectedDnsProfile.PrimaryDns,
                SelectedDnsProfile.SecondaryDns,
                cancellationToken);

            RunOnUI(() =>
            {
                if (ok)
                {
                    DnsStatusMessage = string.Format("Đã chuyển sang {0} thành công.".T(), SelectedDnsProfile.Name);
                    _notificationService?.ShowSuccess("DNS Switcher".T(), DnsStatusMessage);
                }
                else
                {
                    DnsStatusMessage = "Không thể đổi DNS. Cần quyền Quản trị viên (Administrator).".T();
                    _notificationService?.ShowError("DNS Switcher".T(), DnsStatusMessage);
                }
                HasDnsBackup = _engine.HasDnsBackup;
            });
            RefreshActiveAdapter();
        }
        catch (OperationCanceledException)
        {
            DnsStatusMessage = "Đã hủy thao tác đổi DNS.".T();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.ApplySelectedDnsAsync", ex);
            DnsStatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsApplyingDns = false;
        }
    }

    [RelayCommand]
    public async Task RestoreDhcpDnsAsync(CancellationToken cancellationToken = default)
    {
        if (IsApplyingDns || IsBusy) return;

        IsApplyingDns = true;
        DnsStatusMessage = "Đang đặt lại DNS tự động (DHCP)...".T();

        try
        {
            bool ok = await _engine.ApplyDnsProfileAsync("DHCP (Automatic)", "", "", cancellationToken);
            RunOnUI(() =>
            {
                if (ok)
                {
                    DnsStatusMessage = "Đã khôi phục DNS tự động (DHCP) thành công.".T();
                    _notificationService?.ShowSuccess("DNS Switcher".T(), DnsStatusMessage);
                }
                else
                {
                    DnsStatusMessage = "Không thể khôi phục DHCP. Cần quyền Quản trị viên.".T();
                    _notificationService?.ShowError("DNS Switcher".T(), DnsStatusMessage);
                }
                HasDnsBackup = _engine.HasDnsBackup;
            });
            RefreshActiveAdapter();
        }
        catch (OperationCanceledException)
        {
            DnsStatusMessage = "Đã hủy thao tác.".T();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.RestoreDhcpDnsAsync", ex);
            DnsStatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsApplyingDns = false;
        }
    }

    [RelayCommand]
    public async Task RestoreDnsBackupAsync(CancellationToken cancellationToken = default)
    {
        if (IsApplyingDns || IsBusy) return;

        IsApplyingDns = true;
        DnsStatusMessage = "Đang khôi phục cấu hình DNS trước đó...".T();

        try
        {
            bool ok = await _engine.RestoreDnsBackupAsync(cancellationToken);
            RunOnUI(() =>
            {
                if (ok)
                {
                    DnsStatusMessage = "Đã khôi phục DNS theo bản sao lưu trước đó.".T();
                    _notificationService?.ShowSuccess("DNS Switcher".T(), DnsStatusMessage);
                }
                else
                {
                    DnsStatusMessage = "Khôi phục bản sao lưu thất bại.".T();
                    _notificationService?.ShowError("DNS Switcher".T(), DnsStatusMessage);
                }
            });
            RefreshActiveAdapter();
        }
        catch (OperationCanceledException)
        {
            DnsStatusMessage = "Đã hủy thao tác.".T();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.RestoreDnsBackupAsync", ex);
            DnsStatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsApplyingDns = false;
        }
    }

    // ==================== 1-CLICK RESCUE & REPAIR COMMANDS ====================
    [RelayCommand]
    public async Task FlushDnsQuickAsync(CancellationToken cancellationToken = default)
    {
        if (IsRepairing || IsBusy) return;

        IsRepairing = true;
        RepairStatusMessage = "Đang xóa bộ nhớ đệm DNS...".T();

        try
        {
            bool ok = await _engine.FlushDnsAsync();
            RunOnUI(() =>
            {
                RepairStatusMessage = ok
                    ? "Đã xóa bộ nhớ đệm phân giải DNS (ipconfig /flushdns) thành công.".T()
                    : "Lỗi khi xóa bộ nhớ đệm DNS.".T();

                if (ok)
                    _notificationService?.ShowSuccess("Flush DNS".T(), RepairStatusMessage);
                else
                    _notificationService?.ShowWarning("Flush DNS".T(), RepairStatusMessage);
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.FlushDnsQuickAsync", ex);
            RepairStatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsRepairing = false;
        }
    }

    [RelayCommand]
    public async Task ReleaseRenewIpQuickAsync(CancellationToken cancellationToken = default)
    {
        if (IsRepairing || IsBusy) return;

        IsRepairing = true;
        RepairStatusMessage = "Đang giải phóng và xin cấp lại địa chỉ IP (Release / Renew)...".T();

        try
        {
            bool ok = await _engine.ReleaseRenewIpAsync();
            RunOnUI(() =>
            {
                RepairStatusMessage = ok
                    ? "Đã cấp lại địa chỉ IP mới từ DHCP Server thành công.".T()
                    : "Cấp lại IP thất bại hoặc thiết bị không dùng DHCP.".T();

                if (ok)
                    _notificationService?.ShowSuccess("Renew IP".T(), RepairStatusMessage);
                else
                    _notificationService?.ShowWarning("Renew IP".T(), RepairStatusMessage);
            });
            RefreshActiveAdapter();
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.ReleaseRenewIpQuickAsync", ex);
            RepairStatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsRepairing = false;
        }
    }

    [RelayCommand]
    public async Task ResetWinsockAndTcpIpQuickAsync(CancellationToken cancellationToken = default)
    {
        if (IsRepairing || IsBusy) return;

        IsRepairing = true;
        RepairStatusMessage = "Đang đặt lại Winsock Catalog và ngăn xếp TCP/IP...".T();

        try
        {
            bool ok = await _engine.ResetWinsockAndTcpIpAsync(cancellationToken);
            RunOnUI(() =>
            {
                RequiresRestart = true;
                RepairStatusMessage = ok
                    ? "Đã đặt lại Winsock Catalog & TCP/IP. BẮT BUỘC KHỞI ĐỘNG LẠI MÁY ĐỂ ÁP DỤNG.".T()
                    : "Đặt lại Winsock/TCP thất bại. Cần quyền Quản trị viên.".T();

                if (ok)
                    _notificationService?.ShowWarning("Reset Network Stack".T(), RepairStatusMessage);
                else
                    _notificationService?.ShowError("Reset Network Stack".T(), RepairStatusMessage);
            });
        }
        catch (Exception ex)
        {
            CrashLogger.LogException("NetworkViewModel.ResetWinsockAndTcpIpQuickAsync", ex);
            RepairStatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsRepairing = false;
        }
    }
}

