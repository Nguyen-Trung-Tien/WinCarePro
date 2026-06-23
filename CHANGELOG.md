# Changelog

All notable changes to **WinCare Pro** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] - 2026-06-23

### Added
- **System Notifications & Activity Log** (NotificationPage): A new system log explorer allowing users to search, filter, and export system diagnostic/action logs to CSV.
- **CPU Temperature Tracker**: Added real-time CPU temperature monitoring and formatted display directly in the Dashboard.
- **Quick Stats Strip**: Integrated instant view badges for Uptime, Network, Apps, and Junk cleanup on the main dashboard.
- **Glassmorphism UI Upgrade**: Added high-end modern XAML styles (backdrop blur, neon glows, drop shadows, dynamic color badges) to `App.xaml` resource dictionary.
- **Real-Time Clock & Version Badge**: Embedded Clock ticker and active `v3.0` version indicator in Custom Titlebar.
- **Engines DI Registrations**: Registered all telemetry, cleanup, updates, security, registry, hardware and backup engines as singletons in `App.xaml.cs` to enable full architecture testability.

### Fixed
- **Hardcoded Path Bug**: Removed developer's hardcoded path `D:\WinCare\update.json` in `MainWindow.xaml.cs`. Replaced with environment relative base path using `AppDomain.CurrentDomain.BaseDirectory`.
- **Mock Performance Monitors**: Replaced random number generation for GPU and Disk metrics in `DashboardViewModel` with real WMI system query metrics (`Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine`).
- **Memory Leak**: Implemented proper `IDisposable` pattern inside ViewModels to cleanly release system timers, WMI queries, and sensor threads on page unload.
- **XAML Single Child Constraint**: Fixed the `CpuCard` `Border` containing multiple elements compilation issue by introducing parent `StackPanel`.

### Changed
- **Software Updater**: Updated versions of git, discord, chrome, steam, etc., to 2026 releases and added 5 new packages: VLC, 7-Zip, Notepad++, Python, Zoom.

---

## [2.0.5] - 2025-11-12
- Initial Release of WinCare Pro telemetry diagnostics and basic cleanups.
