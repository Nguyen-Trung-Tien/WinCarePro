# Changelog

All notable changes to **WinCare Pro** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [4.9.3] - 2026-09-16 (Orion Maintenance, SHA-256 Update Fix & System Hardening)

### Security & Updater Integrity
- **SHA-256 Checksum Automated Synchronization:** Khắc phục triệt để lỗi mã SHA-256 mismatch khi cập nhật phần mềm. Tự động tính toán mã băm SHA-256 từ file cài đặt và đồng bộ hóa trực tiếp vào `update.json` và CI/CD release workflow qua `scripts/update_sha256.ps1`.
- **Hash Normalization & Sanitize:** Bổ sung `NormalizeHash` trong `UpdateSecurityValidator` tự động loại bỏ tiền tố (`sha256:`, `0x`), khoảng trắng thừa, và chuẩn hóa hex 64 ký tự.
- **Hash-Pinned Open-Source Verification:** Bổ sung cơ chế xác thực toàn vẹn Hash-Pinned cho các gói cập nhật từ GitHub Releases chính thức kết hợp SHA-256 digest 256-bit, tránh việc tự xóa file cài đặt của tác giả khi chưa có chứng chỉ thương mại.
- **Beta / Stable Channel Synchronization:** Đồng bộ hóa logic kiểm tra cập nhật giữa `MainWindow` và `SettingsPage`, đảm bảo kênh Beta tải đúng bản build và so khớp đúng mã SHA-256 tương ứng.

### Stability & Zero-Crash Architecture
- **EventLog Packaging Fix:** Bổ sung `System.Diagnostics.EventLog` vào `WinCarePro.csproj` và chuẩn hóa `PublishSingleFile=false` trong `publish.bat`, `publish_installer.bat` cùng GitHub Actions CD workflow, đảm bảo nạp đầy đủ 468 runtime assemblies cho WinUI 3.
- **Service & Boot Time Defensive Guards:** Bọc khối phòng thủ an toàn quanh `ServiceController` và `GetLastBootTimeSeconds()`, ngăn chặn hiện tượng tê liệt dữ liệu Startup hoặc phát sinh `UnobservedTaskException`.
- **Network Resilience:** Bọc try-catch chống crash khi máy tính mất mạng hoặc rớt kết nối đột ngột trong quá trình kiểm tra cập nhật.

### Quality Assurance & MVVM Architecture
- **Thread Safety & MVVM Hygiene:** Chuẩn hóa phương thức `RunOnUI()` trong `ViewModelBase`, loại bỏ các định nghĩa trùng lặp gây warning CS0108. Bổ sung `finally` guard giải phóng cờ `IsBusy` và `IsScanning`.
- **Comprehensive Test Suite:** 435 / 435 unit test xUnit hoàn thành thành công (100% Passed) với 0 Warning và 0 Error.

---

## [4.9.2] - 2026-09-11 (Orion Security Hardening & UI/UX Modernization Release)

### Security & Filesystem Protection
- **Delivery Optimization Cache Traversal Defense:** Hardened `CleanDeliveryOptimizationCacheAsync` in `SystemOptimizerEngine` against junction/symlink reparse point breakouts. Directory traversal now strictly inspects reparse point attributes and validates directory boundaries with `SafePathGuard.IsPathSafe()` before processing or purging cache files.

### UI/UX & Design System
- **Design System Token Standardization:** Standardized card padding, uniform spacing, and high-contrast typography across all primary views (`MainWindow`, `MainPage`, `DiskPage`, `RegistryPage`, `RepairPage`, `SettingsPage`, `UninstallPage`).
- **Destructive Action Safety:** Introduced a centralized `DangerButtonStyle` in `App.xaml` providing distinct visual feedback for irreversible actions.
- **Reduced Motion & Accessibility:** Integrated `Windows.UI.ViewManagement.UISettings.AnimationsEnabled` tracking in `ThemeManager`, triggering `ReducedMotionChanged` events to seamlessly disable intensive animations when Windows Reduced Motion is enabled.

### Quality Assurance
- **Comprehensive Test Verification:** 422 / 422 automated unit, regression, UI consistency, and security tests passing with 0 warnings and 0 errors.

---

## [4.9.1] - 2026-09-08 (Nova Maintenance & Security Hardening Release)

### Security & Reliability
- **Self-Update Security Hardening:** Mandated SHA-256 verification (rejects missing or mismatched hashes), enforced WinVerifyTrust Authenticode signature checking with publisher verification (`Nguyen Trung Tien`), restricted update downloads to trusted HTTPS release endpoints, eliminated PE-header fallback checks, and implemented fail-closed secure cleanup with comprehensive audit logging.
- **Installer Process Safety:** Updated Inno Setup configuration (`setup.iss`) to restrict `CloseApplicationsFilter` exclusively to `WinCarePro.exe`, preventing unintended termination of third-party applications during installation.
- **UndoManager Registry Rollback Hardening:** Enforced `SafeRegistryGuard` pre-validation before registry write/delete operations in `UndoManagerService`, safely rejecting protected keys, critical startup values, and malformed snapshots with audit tracking.
- **Robust System Error Handling:** Eliminated silent catch blocks across updater, registry, startup, and background services, logging actionable diagnostics to prevent false-positive success reporting.

---

## [4.9.0] - 2026-09-05 (Nova Production Hardening & Safety Architecture Release)

### What's New
- **SafeRegistryGuard Enterprise Barrier:** Centralized protection for Windows registry hives (`HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`), critical OS keys (`SYSTEM\CurrentControlSet`, `Winlogon`, `Image File Execution Options`), and security-critical values (`Shell`, `Userinit`, `AppInit_DLLs`). Key deletions strictly require depth ≥ 3 levels.
- **ServiceSafetyService Fail-Safe Guard:** Kernel and critical operating system services (`RpcSs`, `WinDefend`, `SamSs`, `PlugPlay`, `RpcEptMapper`, `DcomLaunch`) are safeguarded against accidental stopping or disabling in Startup & Services Manager.
- **Full Cancellation & Lifecycle Hardening:** Integrated `CancellationToken` throughout scan, batch uninstall, leftover purge, registry repair, and AI diagnostics. Pages automatically invoke `ViewModel.Cleanup()` on `OnNavigatedFrom`, preventing background tasks from modifying detached or disposed views.
- **UI Dispatcher Thread-Safety:** Background SQLite notification inserts and engine progress events are strictly marshalled to the UI thread via `App.MainDispatcherQueue.TryEnqueue()`, completely eliminating cross-thread COM exceptions.
- **Standardized OperationResult & Error Propagation:** Unified `OperationResult` and `OperationResult<T>` with rich `Exception` propagation, `HasWarnings`, and `HasErrors` properties across all engine boundaries.
- **Comprehensive Quality Milestone:** 300 / 300 automated unit and regression tests passing with 0 Warnings and 0 Errors across both Debug and Release configurations.

### Changes
- **SafePathGuard Enhanced:** Made folder cleanup strictly non-recursive (`Delete(false)`) to guarantee directories are only removed when empty, added `C:\ProgramData` to protected exact paths, and verified reparse points before leftover folder deletion.
- **WMI Timeout Hardening:** Configured 5-second timeout and immediate return flags on `ManagementObjectSearcher` queries to prevent thread locking on damaged WMI repositories.
- **Registry Repair Rescan Fix:** Resolved state locking in `RegistryViewModel.RepairSelectedAsync` to allow instant rescan after completing repair operations.

---

## [4.8.0] - 2026-09-03 (Nova Resilience & State Recovery Release)

### What's New
- **Seamless Network Center Navigation:** Full multi-threaded background task resilience in Speed Test, Ping, Traceroute, and DNS Benchmark. Users can switch between modules without freezing the user interface or locking control buttons.
- **Immediate Task Abort & Bandwidth Conservation:** Active network tests and background downloads automatically disconnect when navigating away from the Network Center, instantly saving CPU cycles and internet bandwidth.
- **Tabular Figure Display Formatting:** Telemetry cards for Ping, Jitter, Download, and Upload speeds now utilize monospace tabular numeral typography for smooth, jitter-free live metric monitoring.
- **Universal State Auto-Recovery:** Automatic reset of busy and scanning states across all system maintenance and repair tools upon page re-navigation.

### Changes
- **Memory & Resource Efficiency:** Enhanced background working set trimming when minimized to system tray, maintaining sub-15MB idle RAM usage.
- **Enhanced SafePathGuard Boundary Protections:** Reinforced system and user credential directory protections during duplicate scanning and deep cleanup operations.
- **Refined Application Theme Polish:** Improved high-contrast text legibility and font consistency across light, dark, and Cyberpunk Neon themes.

---

## [4.7.0] - 2026-09-01 (Nova Architecture Optimization Release)

### What's New
- **Unified Global Header Search:** Integrated high-performance fuzzy search indexing across all settings sections with Vietnamese diacritic support directly from the top navigation header.
- **Consolidated System Optimizer:** Streamlined system acceleration, physical RAM working set recovery, and deep responsiveness tuning into a unified, responsive interface.
- **Fluid Visual Transitions:** 120 FPS buttery-smooth navigation transitions with zero-allocation reduced motion support across all Windows displays.

### Changes
- **Streamlined Suite Architecture:** Consolidated system tuning into the unified, high-performance System Optimizer.
- **Modular Settings Interface:** Reorganized system settings into clean, dedicated functional categories for enhanced navigation.

---

## [4.6.0] - 2026-08-31 (Nova Suite Official Release)

### What's New
- **AI WinCare Diagnostics & Predictive Care:** Embedded heuristic analysis across 8 system dimensions, predictive disk full forecasting, automated 1-click health remedies, and desktop diagnostic export.
- **SafePathGuard & Security Core:** System-level protection blocking accidental deletion of Windows system files, credential isolation for browser logins/cookies, and local SQLite audit logging.
- **Hardware Driver Suite & Backup Manager:** Comprehensive hardware component inspection, missing/outdated driver detection, and 1-click driver backup & restore.
- **Third-Party Software Updater:** Automatic background version scanning for installed software and 1-click batch updating via verified packages.
- **Junk Cleaner & Deep App Uninstaller:** Comprehensive temp/cache purging across Windows and modern browsers, plus deep leftover scanner removing orphan AppData files and registry residue.
- **System Optimizer:** Instant physical RAM working set recovery, dedicated gaming mode with Ultimate Performance power plan, and safe system responsiveness tweaks.
- **Network Center Suite:** Real-time speed & latency testing, 1-click secure DNS switching (Cloudflare, Google, AdGuard), and automated TCP/IP network reset tools.
- **Desktop HUD Mini Widget & Aura Glass 2.0:** Floating desktop hardware monitor, 120 FPS fluid composition, Mica & Acrylic dynamic backdrops, and instant settings search.
- **Full Bilingual Localization:** 100% synchronized English and Vietnamese dictionary across all pages and dialogs.

### Changes
- **Optimized Application Startup:** Streamlined initialization flow with parallel database and theme hydration.
- **Lifecycle-Aware Visuals:** Background animation pausing to conserve GPU power when views are inactive.
