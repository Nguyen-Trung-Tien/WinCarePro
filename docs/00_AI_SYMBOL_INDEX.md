# 🧭 WinCare Pro — AI Symbol Index & Architecture Map

> **Objective:** Provide an $O(1)$ instantaneous symbol lookup index for AI Agents.  
> **Mandate:** Guarantee Zero-Hallucination API invocation and eliminate broad codebase file ingestion.

---

## 🏛️ 1. Layered Architecture & Core Symbol Index

### 🛡️ Core Safety Guards & Interop
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `SafePathGuard` | [src/Core/Guards/SafePathGuard.cs](../src/Core/Guards/SafePathGuard.cs) | `bool IsSafeToDelete(string path)`: Blocks deletions of root volumes, system directories, Windows trees, Reparse Points/Symlinks. |
| `SafeRegistryGuard` | [src/Core/Guards/SafeRegistryGuard.cs](../src/Core/Guards/SafeRegistryGuard.cs) | `bool IsSafeToDeleteKey(RegistryKey root, string subKey)`: Blocks Root Hive deletion, enforces subkey depth $\ge 3$, validates safe whitelist. |
| `ServiceSafetyService` | [src/Infrastructure/Security/ServiceSafetyService.cs](../src/Infrastructure/Security/ServiceSafetyService.cs) | `bool IsProtectedService(string serviceName)`: Shields 45+ critical Windows infrastructure services (`RpcSs`, `WinDefend`, `CryptSvc`, ...). |
| `ProcessRunner` | [src/Core/Helpers/ProcessRunner.cs](../src/Core/Helpers/ProcessRunner.cs) | `Task<ProcessResult> RunAsync(string exe, IEnumerable<string> args, CancellationToken ct)`: Zero Command Injection runner using `ArgumentList`. |
| `CryptoHelper` | [src/Infrastructure/Security/CryptoHelper.cs](../src/Infrastructure/Security/CryptoHelper.cs) | `string Protect(string plainText)`, `string Unprotect(string cipherText)`: Secure encryption utilizing Windows DPAPI. |
| `InputSanitizer` | [src/Core/Helpers/InputSanitizer.cs](../src/Core/Helpers/InputSanitizer.cs) | `string SanitizeFileName(string input)`, `string SanitizeCliArg(string input)`: Input normalization and sanitization. |

### 🧩 Base Models & Helpers
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `OperationResult`, `OperationResult<T>` | [src/Core/Models/OperationResult.cs](../src/Core/Models/OperationResult.cs) | `Ok()`, `Ok(T data)`, `Fail(string msg)`, `Fail(Exception ex)`: Standardized result wrapper for fallible operations. |
| `ViewModelBase` | [src/ViewModels/Common/ViewModelBase.cs](../src/ViewModels/Common/ViewModelBase.cs) | Inherits `ObservableObject`. Exposes `RunOnUI(Action)`, `IsBusy`, `OperationState`, `StatusMessage`. Mandatory for all ViewModels. |
| `AppConstants` | [src/Core/AppConstants.cs](../src/Core/AppConstants.cs) | Single Source of Truth (SSOT) for product identity, version (`6.0.0`), codename (`Polaris`), build date, copyright. |
| `UndoManagerEngine` | [src/Engines/Repair/UndoManagerEngine.cs](../src/Engines/Repair/UndoManagerEngine.cs) | Manages Undo rollbacks, restores registry hives and quarantined files safely. |

### 🗄️ Infrastructure & Persistence
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `DbManager` | [src/Infrastructure/Database/DbManager.cs](../src/Infrastructure/Database/DbManager.cs) | Manages SQLite WAL. All operations MUST acquire `lock (_dbLock)`. Exposes `SaveAuditRecord()`, `GetHistory()`, `SaveSetting()`. |
| `CrashLogger` | [src/Infrastructure/Logging/CrashLogger.cs](../src/Infrastructure/Logging/CrashLogger.cs) | `LogError(Exception ex, string context)`: Captures unhandled crashes, stack traces; enforces Zero-Silent Catch. |
| `AuditLogService` | [src/Infrastructure/Logging/AuditLogService.cs](../src/Infrastructure/Logging/AuditLogService.cs) | Records operational telemetry and business lifecycle events by level (Info, Warn, Error). |

### 🌐 System Services
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `TranslationManager` | [src/Infrastructure/Localization/TranslationManager.cs](../src/Infrastructure/Localization/TranslationManager.cs) | Dual-language i18n Singleton. Manages dictionaries in [TranslationManager.Translations.cs](../src/Infrastructure/Localization/TranslationManager.Translations.cs). |
| `ThemeManager` | [src/Infrastructure/Services/ThemeManager.cs](../src/Infrastructure/Services/ThemeManager.cs) | Manages Aura Glass themes (Light, Dark, Windows Default, Mica Alt backdrop). |
| `IconCacheService` | [src/Infrastructure/Caching/IconCacheService.cs](../src/Infrastructure/Caching/IconCacheService.cs) | Caches Win32/UWP application shell icons to eliminate disk I/O bottlenecks and GDI handle leaks. |
| `NavigationService` | [src/Core/Interfaces/IDialogService.cs](../src/Core/Interfaces/IDialogService.cs) | Manages WinUI 3 dialog and navigation services. |
| `UpdateManager` | [src/Infrastructure/Security/UpdateSecurityValidator.cs](../src/Infrastructure/Security/UpdateSecurityValidator.cs) | Handles automated update checks and payload integrity validation via `UpdateSecurityValidator`. |

---

## ⚙️ 2. Business Logic Engines Index

All engines operate completely isolated from WinUI 3, accept `CancellationToken ct = default`, and return `OperationResult<T>`:

| Engine | File Path | Core Domain Logic |
| :--- | :--- | :--- |
| `JunkCleanerEngine` | [src/Engines/Optimization/JunkCleanerEngine.cs](../src/Engines/Optimization/JunkCleanerEngine.cs) | Scans and purges system junk (Temp, Prefetch, Logs, Dumps); shielded by `SafePathGuard`. |
| `RegistryBackupEngine` | [src/Engines/Repair/RegistryBackupEngine.cs](../src/Engines/Repair/RegistryBackupEngine.cs) | Automated registry hive backups and restore point tracking; shielded by `SafeRegistryGuard`. |
| `HardwareDriverEngine` | [src/Engines/Repair/HardwareDriverEngine.cs](../src/Engines/Repair/HardwareDriverEngine.cs) | Hardware specs, hardware telemetry, and device driver package backups via `pnputil.exe` and `dism.exe`. |
| `UninstallEngine` | [src/Engines/Repair/UninstallEngine.cs](../src/Engines/Repair/UninstallEngine.cs) | App inventory detection, deep clean uninstallation with file/registry residual sweeps. |
| `NetworkEngine` | [src/Engines/Monitoring/NetworkEngine.cs](../src/Engines/Monitoring/NetworkEngine.cs) | Bandwidth benchmarking, DNS benchmark/repair, TCP/IP stack resets, DoH configurations via `ProcessRunner`. |
| `ProcessService` | [src/Engines/Monitoring/ProcessService.cs](../src/Engines/Monitoring/ProcessService.cs) | Active process management, memory working set trimming, thread prioritization. |
| `SmartFixEngine` | [src/Engines/Repair/SmartFixEngine.cs](../src/Engines/Repair/SmartFixEngine.cs) | Unified smart repair orchestrator combining junk purge, network stack fix, SFC/DISM repairs. |
| `SoftwareUpdaterEngine`| [src/Engines/Repair/SoftwareUpdaterEngine.cs](../src/Engines/Repair/SoftwareUpdaterEngine.cs) | Detects and downloads updates for software packages with signature verification. |
| `SystemEngine` | [src/Engines/Diagnostics/SystemEngine.cs](../src/Engines/Diagnostics/SystemEngine.cs) | OS integrity restoration, automated SFC (`sfc /scannow`), DISM, and Windows Update component resets. |
| `SystemSnapshotEngine` | [src/Engines/Diagnostics/SystemSnapshotEngine.cs](../src/Engines/Diagnostics/SystemSnapshotEngine.cs) | Diagnostic system snapshots, differential system health audit and rollback tracking. |

---

## 🖥️ 3. WinCare Pro v6.0.0 Presentation Modules (ViewModel & View)

| Module | ViewModel | View (XAML) |
| :--- | :--- | :--- |
| **Dashboard** | [src/ViewModels/Modules/Dashboard/DashboardViewModel.cs](../src/ViewModels/Modules/Dashboard/DashboardViewModel.cs) | [src/Presentation/Views/Dashboard/DashboardPage.xaml](../src/Presentation/Views/Dashboard/DashboardPage.xaml) |
| **System Cleaner** | [src/ViewModels/Modules/Cleaner/CleanerViewModel.cs](../src/ViewModels/Modules/Cleaner/CleanerViewModel.cs) | [src/Presentation/Views/Cleaner/CleanerPage.xaml](../src/Presentation/Views/Cleaner/CleanerPage.xaml) |
| **Deep Junk Cleaner** | [src/ViewModels/Modules/Cleaner/JunkViewModel.cs](../src/ViewModels/Modules/Cleaner/JunkViewModel.cs) | [src/Presentation/Views/Cleaner/JunkPage.xaml](../src/Presentation/Views/Cleaner/JunkPage.xaml) |
| **App Manager** | [src/ViewModels/Modules/Uninstall/UninstallViewModel.cs](../src/ViewModels/Modules/Uninstall/UninstallViewModel.cs) | [src/Presentation/Views/Uninstall/UninstallPage.xaml](../src/Presentation/Views/Uninstall/UninstallPage.xaml) |
| **Software Updater** | [src/ViewModels/Modules/Updates/UpdaterViewModel.cs](../src/ViewModels/Modules/Updates/UpdaterViewModel.cs) | [src/Presentation/Views/Updates/UpdaterPage.xaml](../src/Presentation/Views/Updates/UpdaterPage.xaml) |
| **Network Center** | [src/ViewModels/Modules/Network/NetworkViewModel.cs](../src/ViewModels/Modules/Network/NetworkViewModel.cs) | [src/Presentation/Views/Network/NetworkPage.xaml](../src/Presentation/Views/Network/NetworkPage.xaml) |
| **System Repair** | [src/ViewModels/Modules/Repair/RepairViewModel.cs](../src/ViewModels/Modules/Repair/RepairViewModel.cs) | [src/Presentation/Views/Repair/RepairPage.xaml](../src/Presentation/Views/Repair/RepairPage.xaml) |
| **Notifications** | [src/Presentation/Views/Notifications/NotificationPage.xaml.cs](../src/Presentation/Views/Notifications/NotificationPage.xaml.cs) | [src/Presentation/Views/Notifications/NotificationPage.xaml](../src/Presentation/Views/Notifications/NotificationPage.xaml) |
| **Settings & Guides**| [src/ViewModels/Modules/Settings/SettingsViewModel.cs](../src/ViewModels/Modules/Settings/SettingsViewModel.cs) | [src/Presentation/Views/Settings/SettingsPage.xaml](../src/Presentation/Views/Settings/SettingsPage.xaml) |

---

## 🧪 4. Testing & Verification Suite

All unit test fixtures reside in [WinCarePro.Tests/](../WinCarePro.Tests):
- **SQLite Test Invariant:** Tests mutating database tables MUST configure the attribute:
  ```csharp
  [Collection(nameof(DatabaseTestCollection))]
  ```
  to eliminate concurrency contention on SQLite file locks (`lock (_dbLock)`).
- **Targeted Execution Standard (AI Standard):**
  ```powershell
  dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~ModuleName" --logger "console;verbosity=minimal"
  ```

---

## 🧰 5. Specialized Workspace Skills Index

Consult specialized runbooks at [.agents/skills/README.md](../.agents/skills/README.md):
- **Architecture & Modularity:** [wincare-module-creator](../.agents/skills/wincare-module-creator/SKILL.md) • [wincare-ci-packaging](../.agents/skills/wincare-ci-packaging/SKILL.md)
- **System & Safety Armor:** [wincare-safe-ops](../.agents/skills/wincare-safe-ops/SKILL.md) • [wincare-db-wal](../.agents/skills/wincare-db-wal/SKILL.md) • [wincare-error-resilience](../.agents/skills/wincare-error-resilience/SKILL.md)
- **Presentation & Threading:** [wincare-concurrency-guard](../.agents/skills/wincare-concurrency-guard/SKILL.md) • [wincare-ui-aura](../.agents/skills/wincare-ui-aura/SKILL.md) • [wincare-i18n-sync](../.agents/skills/wincare-i18n-sync/SKILL.md)
- **Quality & Token Economy:** [wincare-test-verifier](../.agents/skills/wincare-test-verifier/SKILL.md) • [wincare-token-guard](../.agents/skills/wincare-token-guard/SKILL.md)


