# Changelog

All notable changes to **WinCare Pro** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
