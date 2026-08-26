# Changelog

All notable changes to **WinCare Pro** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

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
