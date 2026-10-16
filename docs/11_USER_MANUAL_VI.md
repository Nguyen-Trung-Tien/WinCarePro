# 📖 11. End-User Manual (WinCare Pro User Manual)

> [⬅️ 10. Developer Onboarding Guide](10_DEVELOPER_ONBOARDING_GUIDE.md) • [🏠 Docs Index](README.md) • **Chapter 11** • [Return to Start: 01. System Architecture ➡️](01_SYSTEM_ARCHITECTURE.md)

Welcome to **WinCare Pro Suite** — the next-generation desktop system optimization, security hardening, diagnostics, and performance platform for Windows.

---

## 📥 1. Installation & Initial Launch

### 1.1. System Requirements
- **Operating System:** Windows 10 (Version 2004 / Build 19041+) or Windows 11 (64-bit).
- **Processor (CPU):** Intel Core i3 / AMD Ryzen 3 or higher.
- **Memory (RAM):** 2 GB minimum (4 GB+ recommended).
- **Free Storage Space:** 150 MB.

### 1.2. Installation Procedure
1. Download `WinCareProSetup.exe` from the official repository releases.
2. Double-click the installer executable. When prompted by **User Account Control (UAC)**, click **Yes** to grant administrative privileges.
3. Follow the on-screen installation wizard and click **Finish** to launch WinCare Pro.

---

## 🧭 2. Detailed Module Operations

### 2.1. 📊 System Telemetry & Health Scoring (Dashboard)
- **Primary Dashboard:** Visualizes real-time metrics for CPU, RAM, storage volumes, and network throughput.
- **Composite Health Score:**
  - **90 - 100 (Emerald Green):** System is in optimal condition.
  - **70 - 89 (Amber Yellow):** Non-critical junk files or background processes require attention.
  - **Below 70 (Coral Red):** High resource contention, disk space exhaustion, or unpatched security vulnerabilities detected.
- **"1-Click Quick Optimize":** Reclaims working set RAM and purges safe temporary caches immediately.

---

### 2.2. 🤖 AI-Powered Diagnostics (AI WinCare Assistant)
- **Step 1:** Select **AI WinCare** from the left navigation menu.
- **Step 2:** Click **"Run AI Diagnostics"**. The engine executes 8 concurrent heuristic diagnostic scans across system integrity, registry consistency, disk performance, and security posture.
- **Step 3:** Review detected issues and click **"Fix"** on individual items, or select **"Fix All"** to trigger automated remediation.

---

### 2.3. 🧹 Junk File Cleaner (Junk Cleaner)
- **Step 1:** Navigate to **Junk Cleaner**.
- **Step 2:** Check desired target categories (Windows Temp files, browser caches, error logs, Recycle Bin).
- **Step 3:** Click **"Scan"** to compute recoverable disk capacity without modifying files.
- **Step 4:** Click **"Clean"**. The `SafePathGuard` shield automatically preserves browser credentials, session tokens, and core system folders.

---

### 2.4. 📦 Software Uninstaller & Leftover Cleaner (App Uninstaller)
- **Step 1:** Navigate to **App Uninstaller**.
- **Step 2:** Filter or search for target installed software.
- **Step 3:** Click **"Uninstall"** to invoke the vendor uninstaller.
- **Step 4:** Upon uninstallation completion, WinCare Pro triggers **Deep Scan** to identify orphaned files in `%AppData%` and lingering registry keys. Click **"Clean Leftovers"** to finalize removal.

---

### 2.5. 🌐 Network Diagnostics & Optimization (Network Center)
- **Speed Test:** Click **"Speed Test"** to benchmark ICMP Ping latency, download speed, and upload throughput.
- **1-Click DNS Overrides:** Select a preferred public DNS provider (Cloudflare 1.1.1.1, Google 8.8.8.8, AdGuard Ad-blocking) and click **"Apply DNS"**.
- **Network Stack Recovery:** If network connectivity fails, click **"Reset Network"** to flush local DNS resolver caches and reset the Winsock TCP/IP stack.

---

### 2.6. 🛠️ Windows System Repair (System Repair)
- **Corrupt File Repair (SFC Scan):** Click **"Run SFC Scan"** (`sfc /scannow`).
- **Component Store Restoration (DISM):** Click **"Restore DISM Health"** to service and repair the Windows component store using official Microsoft images.
- **Windows Update Repair:** Click **"Fix Windows Update"** to resolve stalled update services, clear SoftwareDistribution caches, and re-register update components.

---

### 2.7. 🎮 Gaming Optimization (Gaming Turbo 2.0)
- **Step 1:** Navigate to **Gaming Turbo**.
- **Step 2:** Toggle **"Game Mode" ON**.
- **Automated Optimizations:**
  - Engages the **Ultimate Performance** power profile.
  - Reclaims physical working set memory.
  - Temporarily suppresses background tasks causing frame stutter.
  - Automatically restores normal power and background state when toggled OFF.

---

### 2.8. 🪟 Desktop HUD Floating Widget
- Open **Settings** or click the **Widget** icon on the TitleBar to spawn the floating HUD window.
- The widget renders real-time CPU, RAM, and network metrics on the Windows desktop.
- Drag and position the widget freely across primary and secondary monitors.

---

## ❓ 3. Frequently Asked Questions (FAQ & Troubleshooting)

**Q1: Will WinCare Pro accidentally delete my personal documents?**  
*Answer:* Absolutely not. WinCare Pro enforces the `SafePathGuard` safety engine, which completely excludes user personal folders (`Documents`, `Desktop`, `Pictures`, `Videos`) and shields sensitive credential stores and browser passwords.

**Q2: Why does WinCare Pro require Administrator privileges?**  
*Answer:* Low-level OS capabilities — such as component store servicing (SFC/DISM), physical RAM flushing, HKLM registry modifications, and network interface resets — require Windows administrative privileges to execute safely.

**Q3: Can I roll back changes if needed?**  
*Answer:* Yes. Prior to registry modifications or service state adjustments, WinCare Pro automatically records snapshots and backups. Navigate to **Undo / Snapshots** to revert the system to an earlier state at any time.

---

> [⬅️ 10. Developer Onboarding Guide](10_DEVELOPER_ONBOARDING_GUIDE.md) • [🏠 Docs Index](README.md) • **Chapter 11** • [Return to Start: 01. System Architecture ➡️](01_SYSTEM_ARCHITECTURE.md)

