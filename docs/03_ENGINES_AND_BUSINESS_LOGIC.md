# ⚙️ 03. Business Logic Engines Architecture

> [⬅️ 02. Core Modules](02_CORE_MODULES_DETAILED.md) • [🏠 Documentation Index](README.md) • **Chapter 03** • [Next: 04. Database & Storage Architecture ➡️](04_DATABASE_AND_STORAGE.md)

This document specifies the internal architecture, method contracts, algorithms, and operational mechanisms governing all business logic classes located within `Engines/`.

---

## 🏗️ Engine Subsystem Taxonomy

Business logic engines are partitioned across 4 functional domains:

```
Engines/
├── Diagnostics/         # AI heuristic analysis, health scoring, predictive models, system repairs
├── Monitoring/          # Hardware telemetry, process monitoring, network benchmarking
├── Optimization/        # Junk deletion, memory compaction, disk analysis, startup management
└── Repair/              # Deep uninstallation, registry backups, security policies, drivers
```

---

## 1. 🔍 Diagnostics Engines Group

### 1.1. `AiDiagnosticsEngine`
- **File:** [Engines/Diagnostics/AiDiagnosticsEngine.cs](../Engines/Diagnostics/AiDiagnosticsEngine.cs)
- **Objective:** Cloud-free, offline heuristic diagnostic engine evaluating host system stability.
- **Core Algorithms:**
  - `RunFullDiagnosticAsync(IProgress<DiagnosticProgress> progress, CancellationToken ct)`: Dispatches 8 parallel diagnostic workers:
    1. *Memory Pressure*: Verifies whether physical RAM utilization exceeds 85% with high Paging File commit ratios.
    2. *Storage Warning*: Flags system drive (`C:`) free capacity dipping below 15%.
    3. *Junk Volume*: Estimates ephemeral junk file accumulation across temporary volumes.
    4. *Process Hogging*: Identifies runaway non-system processes sustaining prolonged CPU saturation.
    5. *Security Flaws*: Audits real-time status of Windows Defender, Windows Firewall, and User Account Control (UAC).
    6. *Network Latency*: Evaluates round-trip ICMP packet latency and DNS resolution responsiveness.
    7. *Startup Congestion*: Analyzes concentration of High-Impact startup applications.
    8. *Residual Folders*: Scans for orphaned AppData directories belonging to previously uninstalled software.
  - `ExecuteFixActionAsync(DiagnosticItem item)`: Executes targeted automated remediation for identified diagnostic findings.

### 1.2. `AiWinCareScoringEngine`
- **File:** [Engines/Diagnostics/AiWinCareScoringEngine.cs](../Engines/Diagnostics/AiWinCareScoringEngine.cs)
- **Objective:** Computes a composite system health score ranging from 0 to 100 points.
- **Weighted Scoring Formula:**
  $$\text{Score} = 100 - (\mathbf{W}_{ram} \cdot P_{ram} + \mathbf{W}_{disk} \cdot P_{disk} + \mathbf{W}_{sec} \cdot P_{sec} + \mathbf{W}_{junk} \cdot P_{junk} + \mathbf{W}_{boot} \cdot P_{boot})$$
  - Factor Weights: Security ($\mathbf{W}_{sec} = 0.30$), Storage ($\mathbf{W}_{disk} = 0.25$), RAM ($\mathbf{W}_{ram} = 0.20$), Junk ($\mathbf{W}_{junk} = 0.15$), Boot Delay ($\mathbf{W}_{boot} = 0.10$).

### 1.3. `PredictiveAnalysisEngine`
- **File:** [Engines/Diagnostics/PredictiveAnalysisEngine.cs](../Engines/Diagnostics/PredictiveAnalysisEngine.cs)
- **Objective:** Projects system drive capacity exhaustion dates and SSD wear trends.
- **Key Method:**
  - `ForecastDiskFullDays(string driveLetter)`: Samples historical storage utilization rates stored in SQLite to compute GB/day growth vectors and extrapolate remaining days before drive depletion.

### 1.4. `SystemEngine`
- **File:** [Engines/Diagnostics/SystemEngine.cs](../Engines/Diagnostics/SystemEngine.cs)
- **Objective:** Orchestrates native Windows servicing and maintenance binaries via [ProcessRunner.cs](../Core/Helpers/ProcessRunner.cs).
- **Key Methods:**
  - `RunSfcScanAsync(IProgress<string> outputProgress)`: Asynchronously executes `sfc.exe /scannow` and parses stdout completion percentages.
  - `RunDismRestoreHealthAsync(IProgress<string> outputProgress)`: Executes `DISM.exe /Online /Cleanup-Image /RestoreHealth`.
  - `RunDismComponentCleanupAsync()`: Reclaims `WinSxS` component storage via `/StartComponentCleanup /ResetBase`.
  - `RepairWindowsUpdateAsync()`: Restarts Windows Update services and purges corrupted `%windir%\SoftwareDistribution` caches.

---

## 2. 📊 Monitoring Engines Group

### 2.1. `ProcessService`
- **File:** [Engines/Monitoring/ProcessService.cs](../Engines/Monitoring/ProcessService.cs)
- **Objective:** Ingests telemetry metrics and manages lifecycle states across all active system processes.
- **Key Methods:**
  - `GetRunningProcessesAsync()`: Enumerates running processes with PID, Working Set memory, CPU %, process name, image path, and cached icon handles (`IconCacheService`).
  - `KillProcessAsync(int processId)`: Safely terminates target processes via native Win32 `OpenProcess` & `TerminateProcess` calls.
  - `SetProcessPriority(int processId, ProcessPriorityClass priority)`: Adjusts process scheduling priority classes (leveraged by Gaming Turbo).

### 2.2. `NetworkEngine` (Modular Partial Architecture)
- **Primary Contract:** [Engines/Monitoring/NetworkEngine.cs](../Engines/Monitoring/NetworkEngine.cs)
- **Component Implementations:**
  - [NetworkEngine.SpeedTest.cs](../Engines/Monitoring/NetworkEngine.SpeedTest.cs): Downloads fixed-size test buffers over `HttpClient` using buffered streams to determine actual Mbps throughput and jitter.
  - [NetworkEngine.DnsBenchmark.cs](../Engines/Monitoring/NetworkEngine.DnsBenchmark.cs): Dispatches UDP DNS query sockets to public resolver endpoints, calculating Round-Trip Time (RTT ms).
  - [NetworkEngine.Repair.cs](../Engines/Monitoring/NetworkEngine.Repair.cs): Sequentially runs `ipconfig /flushdns`, `netsh winsock reset`, `netsh int ip reset`, and `ipconfig /renew`.

---

## 3. ⚡ Optimization Engines Group

### 3.1. `JunkCleanerEngine`
- **File:** [Engines/Optimization/JunkCleanerEngine.cs](../Engines/Optimization/JunkCleanerEngine.cs)
- **Objective:** Deep recursive enumeration and safe deletion of ephemeral cache and temporary files.
- **Traversal Mechanics:**
  - Employs `Directory.EnumerateFiles` with `SafePathGuard` filtering during recursive traversal.
  - Verifies file extensions and folder paths against blacklists before invoking `File.Delete`.
  - Catches and skips `UnauthorizedAccessException` and `IOException` without aborting active scan workflows.

### 3.2. `SystemOptimizerEngine`
- **File:** [Engines/Optimization/SystemOptimizerEngine.cs](../Engines/Optimization/SystemOptimizerEngine.cs)
- **Objective:** Registry performance tuning, memory working set reclamation, and power plan configuration.
- **Key Methods:**
  - `FlushMemoryWorkingSet()`: Iterates through active user processes and invokes Win32 `EmptyWorkingSet(hProcess)` via `psapi.dll`, releasing unreferenced physical RAM.
  - `ApplySystemTweaksAsync(List<SystemTweak> tweaks)`: Writes DWORD/String values into `HKEY_CURRENT_USER` or `HKEY_LOCAL_MACHINE`.
  - `EnableUltimatePerformancePowerPlan()`: Executes `powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61`.

### 3.3. `DiskEngine`
- **File:** [Engines/Optimization/DiskEngine.cs](../Engines/Optimization/DiskEngine.cs)
- **Key Methods:**
  - `GetDriveInfoList()`: Retrieves total capacity, free space, and volume formats (NTFS/FAT32).
  - `GetSmartAttributesAsync(string driveLetter)`: Reads S.M.A.R.T attributes via WMI `MSStorageDriver_FailurePredictData` and Win32 `DeviceIoControl`.
  - `FindDuplicateFilesAsync(string searchPath, CancellationToken ct)`: 2-stage duplicate detection algorithm: Stage 1 groups files by byte length; Stage 2 computes SHA-256 block hashes on identical-length files to guarantee 100% collision-free matching.

### 3.4. `StartupEngine`
- **File:** [Engines/Optimization/StartupEngine.cs](../Engines/Optimization/StartupEngine.cs)
- **Key Methods:**
  - `GetStartupItemsAsync()`: Enumerates autorun registrations across 4 locations:
    1. `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`
    2. `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
    3. User Startup Folder (`shell:startup` / `%AppData%\Microsoft\Windows\Start Menu\Programs\Startup`)
    4. Common Startup Folder (`shell:common startup` / `%ProgramData%\Microsoft\Windows\Start Menu\Programs\Startup`)
  - `ToggleStartupItem(StartupItem item, bool isEnabled)`: Toggles entries by moving registry keys to `RunDisabled` or modifying shortcut attributes.
  - **Defensive Safeguards:** Validates mutations via [SafeRegistryGuard.cs](../Core/Helpers/SafeRegistryGuard.cs) and [ServiceSafetyService.cs](../Core/Helpers/ServiceSafetyService.cs) to protect essential system services and autoruns.

---

## 4. 🛠️ Repair & Security Engines Group

### 4.1. `UninstallEngine`
- **Files:** [UninstallEngine.cs](../Engines/Repair/UninstallEngine.cs), [UninstallEngine.Scanning.cs](../Engines/Repair/UninstallEngine.Scanning.cs), [UninstallEngine.Leftovers.cs](../Engines/Repair/UninstallEngine.Leftovers.cs)
- **3-Step Uninstallation Pipeline:**
  1. *Launch Native Uninstaller:* Invokes `UninstallString` or `QuietUninstallString` retrieved from Registry hives.
  2. *Wait for Process Exit:* Monitors native uninstaller PID under bounded timeouts.
  3. *Deep Leftover Sweep:* Recursively scans for directories and registry keys matching Software Name or Publisher, presenting residual candidates to users before deletion.

### 4.2. `SecurityPrivacyEngine`
- **File:** [Engines/Repair/SecurityPrivacyEngine.cs](../Engines/Repair/SecurityPrivacyEngine.cs)
- **Key Methods:**
  - `GetSecurityStatusAsync()`: Audits Defender state via WMI `root\SecurityCenter2` and Registry.
  - `ToggleTelemetry(bool disable)`: Sets policy keys `AllowTelemetry = 0` in `HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection`.
  - `VerifyDigitalSignature(string filePath)`: Invokes native Win32 `WinVerifyTrust` via `wintrust.dll` to validate Authenticode digital signatures.

### 4.3. `SoftwareUpdaterEngine`
- **File:** [Engines/Repair/SoftwareUpdaterEngine.cs](../Engines/Repair/SoftwareUpdaterEngine.cs)
- **Key Methods:**
  - `ScanForUpdatesAsync()`: Executes `winget upgrade --include-unknown` and parses package tables: Name, ID, Current Version, Available Version.
  - `UpdatePackageAsync(string packageId, IProgress<string> logProgress)`: Invokes `winget upgrade --id <packageId> --silent --accept-package-agreements --accept-source-agreements`.

### 4.4. `RegistryBackupEngine`
- **File:** [Engines/Repair/RegistryBackupEngine.cs](../Engines/Repair/RegistryBackupEngine.cs)
- **Key Methods:**
  - `BackupKey(RegistryHive hive, string subKeyPath, string destinationRegFile)`: Invokes `reg.exe export` via ProcessRunner to export standard `.reg` snapshots.
  - `RestoreBackup(string regFilePath)`: Invokes `reg.exe import` to rollback state.

### 4.5. `HardwareDriverEngine`
- **File:** [Engines/Repair/HardwareDriverEngine.cs](../Engines/Repair/HardwareDriverEngine.cs)
- **Key Methods:**
  - `GetInstalledDriversAsync()`: Queries WMI `Win32_PnPSignedDriver` for device driver inventories, versions, dates, and signers.
  - `BackupDriversAsync(string backupDirectory)`: Executes `dism.exe /online /export-driver /destination:<path>` to export third-party drivers.

---

> [⬅️ 02. Core Modules](02_CORE_MODULES_DETAILED.md) • [🏠 Documentation Index](README.md) • **Chapter 03** • [Next: 04. Database & Storage Architecture ➡️](04_DATABASE_AND_STORAGE.md)
