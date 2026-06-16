# 🚀 WinCare Pro Suite

**WinCare Pro** is a modern, lightweight, and comprehensive Windows Maintenance, Optimization, and Repair Suite built using **WinUI 3 (Windows App SDK)**, **.NET 10.0**, and **SQLite**. 

It provides users with an all-in-one utility dashboard to scan, diagnose, optimize, and secure their Windows operating system with a sleek, clean Fluent Design user interface.

---

## ✨ Key Features

### 🧹 Care & Cleanup
* **Junk Cleaner:** Scan and safely purge temporary files, log dumps, browser caches, and redundant Windows Update installers.
* **System Repair:** Run automated DISM/SFC system component scans and repairs.
* **Network Center:** Analyze network interface states, active adapters, ping response times, and connection health.

### ⚙️ System Tuning & Optimization
* **Startup & Services:** Inspect startup application overhead and manage active Windows system services.
* **Process Manager:** Real-time process listing with resource footprints (CPU/RAM) and options to terminate tasks.
* **Disk Tools:** View raw disk S.M.A.R.T. health diagnostics, scan for duplicate files, analyze storage allocations, and clear empty directories.

### 🛡️ Safety & Audits
* **Security & Privacy:** Check Windows Defender real-time protection, UAC level states, firewall configurations, and local system access audits.
* **Registry & Backup:** Quick local registry health scans and secure backup/restore operations.
* **Hardware Specifications:** Display motherboard, CPU, RAM, GPU, storage interfaces, and OS build versions.

### 📊 Reports & Operations
* **Logs & Reports:** Maintain history of all scans and diagnostics, and generate detailed plain text (.txt) or JSON log reports.
* **Auto-Updater:** Built-in checks against the repository metadata to download and install updates seamlessly.

---

## 📦 How to Download & Install

To distribute the app or run it on another machine without setting up the source code, follow these methods:

### Method 1: Download from GitHub Releases (Recommended for Users)
1. Go to the **Releases** section on the right side of this GitHub repository page.
2. Click on the latest version (e.g., `v1.0.0`).
3. Under **Assets**, download **`WinCarePro_Setup.exe`** (or the standalone `WinCarePro.exe`).
4. Open the downloaded file to run or install.

### Method 2: Package standalone version from Source Code (For Developers)
If you have cloned the source code, we have provided an automated build tool:
1. Double-click the **`publish.bat`** file in the root directory.
2. This compiles the project and packages it as a single self-contained application.
3. Open the newly generated **`PublishOutput`** folder and run **`WinCarePro.exe`**.

---

## 🛠️ Build and Development Prerequisites

If you want to modify or compile the codebase manually:
* **Operating System:** Windows 10 (build 19041+) or Windows 11.
* **IDE:** Visual Studio 2022 (with .NET Desktop Development workload).
* **SDK:** .NET 10.0 SDK and Windows App SDK.
