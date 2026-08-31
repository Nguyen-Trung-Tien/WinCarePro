# Changelog

All notable changes to **WinCare Pro** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [4.6.0] - 2026-08-31 (Nova Suite Official Release)

### Added & Enhanced (User Features)
- **AI WinCare Diagnostics & Predictive Care:** Embedded heuristic analysis across 8 system dimensions, predictive disk full forecasting, automated 1-click health remedies, and desktop diagnostic export.
- **SafePathGuard & InputSanitizer Security Core:** System-level protection blocking accidental deletion of Windows system files, credential isolation for browser logins/cookies, and local SQLite audit logging.
- **Hardware Driver Suite & Backup Manager:** Comprehensive hardware component inspection, missing/outdated driver detection, and 1-click driver backup & restore.
- **Third-Party Software Updater:** Automatic background version scanning for installed software and 1-click batch updating via verified packages.
- **Junk Cleaner & Deep App Uninstaller:** Comprehensive temp/cache purging across Windows and modern browsers, plus deep leftover scanner removing orphan AppData files and registry residue.
- **System Optimizer & Gaming Turbo 2.0:** Instant physical RAM working set recovery, dedicated gaming mode with Ultimate Performance power plan, and safe system responsiveness tweaks.
- **Network Center Suite:** Real-time speed & latency testing, 1-click secure DNS switching (Cloudflare, Google, AdGuard), and automated TCP/IP network reset tools.
- **Desktop HUD Mini Widget & Aura Glass 2.0:** Floating desktop hardware monitor, 120 FPS fluid composition, Mica & Acrylic dynamic backdrops, and instant settings search.
- **Full Bilingual Localization:** 100% synchronized English and Vietnamese dictionary across all pages and dialogs.

## [4.5.0] - 2026-08-27 (Nova Release)

### Performance & Modernization
- **Full Architecture & Safety Modernization:** Standardized `DispatcherQueue` fallback, eliminated silent catches with structured `CrashLogger.LogException`, and prevented event listener leaks across all ViewModels.
- **Lifecycle-Aware Visuals:** Paused 3D radar animations on Dashboard navigation unloaded to save background CPU/GPU power.
- **Instantaneous Startup:** Streamlined initialization flow down to ~200ms with parallel database & theme hydration.

## [4.3.0-RC1] - 2026-08-26 (Release Candidate 1)

### Added
- **AI WinCare Engine & Heuristic Diagnostics:** Multi-factor health evaluation combining hardware telemetry, disk status, startup applications, junk metrics, network latency, and security audits.
- **Accessibility & Screen Reader Compliance:** Added `AutomationProperties.Name` and `AutomationProperties.HelpText` to all navigation sidebar items for full Windows Narrator compatibility.
- **Reduced Motion Support:** Added `AreAnimationsEnabled()` checking system accessibility `UISettings.AnimationsEnabled` alongside user settings in `AnimationHelper.cs`.
- **Software Updater Resilience:** 3-attempt exponential retry loop for installer downloads, HTTPS enforcement, and immediate cleanup on signature verification failures.
- **Power Plan Crash Auto-Recovery:** Automatic restoration of standard power scheme following unclean system shutdowns.

### Hardened & Fixed
- **ProcessRunner Stream Synchronization:** Resolved potential race condition on fast-exiting processes by awaiting stream completion to EOF before buffer disposal.
- **Zero-Trust Command Execution:** 100% of internal system tool executions (`sfc`, `dism`, `pnputil`, `powercfg`, `sc`, `netsh`, `ipconfig`, `reg`) now utilize structured parameter arrays.
- **SafePathGuard Filesystem Boundary:** Strict exclusion of Windows roots, System32, SysWOW64, Program Files, User Profiles, and Reparse Point / Junction traversal protection.
- **Memory WorkingSet Transparency:** Clear differentiation between working set trimming (pages moved to standby cache) and physical RAM available.
- **SQLite Concurrency & WAL:** WAL journaling mode, 5000ms busy timeout, compound indexing (`idx_snapshots_cat_key`, `idx_logs_createdat`), and rollback-guarded atomic transactions.

---

## [4.2.0] - 2026-08-15
- Initial production baseline for Windows 10 & 11.
- Junk Cleaner parallel scanning across 12 categories.
- System Optimizer registry tweaks with atomic state snapshots.
