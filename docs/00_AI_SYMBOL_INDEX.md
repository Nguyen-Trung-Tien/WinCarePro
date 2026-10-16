# 🧭 WinCare Pro — AI Symbol Index & Architecture Map

> **Objective:** Provide an $O(1)$ instantaneous symbol lookup index for AI Agents.  
> **Mandate:** Guarantee Zero-Hallucination API invocation and eliminate broad codebase file ingestion.

---

## 🏛️ 1. Layered Architecture & Core Symbol Index

### 🛡️ Core Safety Guards & Interop
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `SafePathGuard` | [Core/Helpers/SafePathGuard.cs](../Core/Helpers/SafePathGuard.cs) | `bool IsSafeToDelete(string path)`: Blocks deletions of root volumes, system directories, Windows trees, Reparse Points/Symlinks. |
| `SafeRegistryGuard` | [Core/Helpers/SafeRegistryGuard.cs](../Core/Helpers/SafeRegistryGuard.cs) | `bool IsSafeToDeleteKey(RegistryKey root, string subKey)`: Blocks Root Hive deletion, enforces subkey depth $\ge 3$, validates safe whitelist. |
| `ServiceSafetyService` | [Core/Helpers/ServiceSafetyService.cs](../Core/Helpers/ServiceSafetyService.cs) | `bool IsProtectedService(string serviceName)`: Shields 45+ critical Windows infrastructure services (`RpcSs`, `WinDefend`, `CryptSvc`, ...). |
| `ProcessRunner` | [Core/Helpers/ProcessRunner.cs](../Core/Helpers/ProcessRunner.cs) | `Task<ProcessResult> RunAsync(string exe, IEnumerable<string> args, CancellationToken ct)`: Zero Command Injection runner using `ArgumentList`. |
| `CryptoHelper` | [Core/Helpers/CryptoHelper.cs](../Core/Helpers/CryptoHelper.cs) | `string Protect(string plainText)`, `string Unprotect(string cipherText)`: Secure encryption utilizing Windows DPAPI. |
| `InputSanitizer` | [Core/Helpers/InputSanitizer.cs](../Core/Helpers/InputSanitizer.cs) | `string SanitizeFileName(string input)`, `string SanitizeCliArg(string input)`: Input normalization and sanitization. |

### 🧩 Base Models & Helpers
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `OperationResult`, `OperationResult<T>` | [Core/Models/OperationResult.cs](../Core/Models/OperationResult.cs) | `Ok()`, `Ok(T data)`, `Fail(string msg)`, `Fail(Exception ex)`: Standardized result wrapper for fallible operations. |
| `ViewModelBase` | [Core/Helpers/ViewModelBase.cs](../Core/Helpers/ViewModelBase.cs) | Inherits `ObservableObject`. Exposes `RunOnUI(Action)`, `IsBusy`, `OperationState`, `StatusMessage`. Mandatory for all ViewModels. |
| `AppConstants` | [Core/AppConstants.cs](../Core/AppConstants.cs) | Single Source of Truth (SSOT) for product identity, version (`5.1.1`), codename (`Polaris`), build date, copyright. |
| `UndoManager` | [Core/Helpers/UndoManager.cs](../Core/Helpers/UndoManager.cs) | Manages Undo rollbacks, restores registry hives and quarantined files safely. |

### 🗄️ Infrastructure & Persistence
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `DbManager` | [Infrastructure/Database/DbManager.cs](../Infrastructure/Database/DbManager.cs) | Manages SQLite WAL. All operations MUST acquire `lock (_dbLock)`. Exposes `SaveAuditRecord()`, `GetHistory()`, `SaveSetting()`. |
| `CrashLogger` | [Infrastructure/Logging/CrashLogger.cs](../Infrastructure/Logging/CrashLogger.cs) | `LogError(Exception ex, string context)`: Captures unhandled crashes, stack traces; enforces Zero-Silent Catch. |
| `AppLogger` | [Infrastructure/Logging/AppLogger.cs](../Infrastructure/Logging/AppLogger.cs) | Records operational telemetry and business lifecycle events by level (Info, Warn, Error). |

### 🌐 System Services
| Class / Symbol | File Path | Core Responsibilities & Canonical Signatures |
| :--- | :--- | :--- |
| `TranslationManager` | [Services/TranslationService/TranslationManager.cs](../Services/TranslationService/TranslationManager.cs) | Dual-language i18n Singleton. Manages dictionaries in [TranslationManager.Translations.cs](../Services/TranslationService/TranslationManager.Translations.cs). |
| `ThemeManager` | [Services/ThemeService/ThemeManager.cs](../Services/ThemeService/ThemeManager.cs) | Manages Aura Glass themes (Light, Dark, Windows Default, Mica Alt backdrop). |
| `IconCacheService` | [Services/IconCacheService.cs](../Services/IconCacheService.cs) | Caches Win32/UWP application shell icons to eliminate disk I/O bottlenecks and GDI handle leaks. |
| `NavigationService` | [Services/NavigationService.cs](../Services/NavigationService.cs) | Manages WinUI 3 page navigation and routing between feature views. |
| `UpdateManager` | [Services/UpdateManager.cs](../Services/UpdateManager.cs) | Handles automated update checks and payload integrity validation via `UpdateSecurityValidator`. |

---

## ⚙️ 2. Business Logic Engines Index

All engines operate completely isolated from WinUI 3, accept `CancellationToken ct = default`, and return `OperationResult<T>`:

| Engine | File Path | Core Domain Logic |
| :--- | :--- | :--- |
| `JunkCleanerEngine` | [Engines/JunkCleanerEngine.cs](../Engines/JunkCleanerEngine.cs) | Scans and purges system junk (Temp, Prefetch, Logs, Dumps); shielded by `SafePathGuard`. |
| `RegistryEngine` | [Engines/RegistryEngine.cs](../Engines/RegistryEngine.cs) | Scans and optimizes Registry; automated backups via `RegistryBackupEngine`; shielded by `SafeRegistryGuard`. |
| `SystemOptimizerEngine`| [Engines/SystemOptimizerEngine.cs](../Engines/SystemOptimizerEngine.cs)| OS performance tuning, Visual Effects configuration, network parameters, and service optimizations. |
| `UninstallEngine` | [Engines/UninstallEngine.cs](../Engines/UninstallEngine.cs) | Program inventory detection, deep clean uninstallation with file/registry residual sweeps. |
| `NetworkCenterEngine` | [Engines/NetworkCenterEngine.cs](../Engines/NetworkCenterEngine.cs) | Bandwidth benchmarking, DNS flush, TCP/IP stack resets, MTU optimization via `ProcessRunner`. |
| `StartupEngine` | [Engines/StartupEngine.cs](../Engines/StartupEngine.cs) | Manages Windows autorun entries (Registry Run, Startup Folders, Task Scheduler). |
| `SystemRepairEngine` | [Engines/SystemRepairEngine.cs](../Engines/SystemRepairEngine.cs) | OS integrity restoration, automated SFC (`sfc /scannow`), DISM (`DISM /Online /Cleanup-Image /RestoreHealth`). |
| `SecurityCenterEngine`| [Engines/SecurityCenterEngine.cs](../Engines/SecurityCenterEngine.cs)| Inspects states for Windows Defender, Windows Firewall, UAC, BitLocker, Windows Update. |
| `AiWinCareEngine` | [Engines/AiWinCareEngine.cs](../Engines/AiWinCareEngine.cs) | Intelligent heuristics engine predicting performance degradation and recommending optimizations. |
| `AiDiagnosticsEngine` | [Engines/AiDiagnosticsEngine.cs](../Engines/AiDiagnosticsEngine.cs)| In-depth system log and hardware diagnostics using structured rule trees. |

---

## 🖥️ 3. Fourteen Presentation Modules (Modules: ViewModel & View)

| Module | ViewModel | View (XAML) |
| :--- | :--- | :--- |
| **Dashboard** | [Modules/Dashboard/DashboardViewModel.cs](../Modules/Dashboard/DashboardViewModel.cs) | [Modules/Dashboard/DashboardView.xaml](../Modules/Dashboard/DashboardView.xaml) |
| **Junk Cleaner** | [Modules/JunkCleaner/JunkCleanerViewModel.cs](../Modules/JunkCleaner/JunkCleanerViewModel.cs) | [Modules/JunkCleaner/JunkCleanerView.xaml](../Modules/JunkCleaner/JunkCleanerView.xaml) |
| **Registry** | [Modules/Registry/RegistryViewModel.cs](../Modules/Registry/RegistryViewModel.cs) | [Modules/Registry/RegistryView.xaml](../Modules/Registry/RegistryView.xaml) |
| **System Optimizer**| [Modules/SystemOptimizer/SystemOptimizerViewModel.cs](../Modules/SystemOptimizer/SystemOptimizerViewModel.cs) | [Modules/SystemOptimizer/SystemOptimizerView.xaml](../Modules/SystemOptimizer/SystemOptimizerView.xaml) |
| **Startup Manager** | [Modules/StartupManager/StartupManagerViewModel.cs](../Modules/StartupManager/StartupManagerViewModel.cs) | [Modules/StartupManager/StartupManagerView.xaml](../Modules/StartupManager/StartupManagerView.xaml) |
| **Uninstall** | [Modules/Uninstall/UninstallViewModel.cs](../Modules/Uninstall/UninstallViewModel.cs) | [Modules/Uninstall/UninstallView.xaml](../Modules/Uninstall/UninstallView.xaml) |
| **System Repair** | [Modules/Repair/SystemRepairViewModel.cs](../Modules/Repair/SystemRepairViewModel.cs) | [Modules/Repair/SystemRepairView.xaml](../Modules/Repair/SystemRepairView.xaml) |
| **Security Center**| [Modules/Security/SecurityCenterViewModel.cs](../Modules/Security/SecurityCenterViewModel.cs) | [Modules/Security/SecurityCenterView.xaml](../Modules/Security/SecurityCenterView.xaml) |
| **Network Center** | [Modules/Network/NetworkCenterViewModel.cs](../Modules/Network/NetworkCenterViewModel.cs) | [Modules/Network/NetworkCenterView.xaml](../Modules/Network/NetworkCenterView.xaml) |
| **AI Assistant** | [Modules/AiAssistant/AiAssistantViewModel.cs](../Modules/AiAssistant/AiAssistantViewModel.cs) | [Modules/AiAssistant/AiAssistantView.xaml](../Modules/AiAssistant/AiAssistantView.xaml) |
| **Desktop Widget** | [Modules/DesktopWidget/DesktopWidgetViewModel.cs](../Modules/DesktopWidget/DesktopWidgetViewModel.cs) | [Modules/DesktopWidget/DesktopWidgetWindow.xaml](../Modules/DesktopWidget/DesktopWidgetWindow.xaml) |
| **Notifications** | [Modules/Notifications/NotificationCenterViewModel.cs](../Modules/Notifications/NotificationCenterViewModel.cs)| [Modules/Notifications/NotificationCenterView.xaml](../Modules/Notifications/NotificationCenterView.xaml)|
| **Settings** | [Modules/Settings/SettingsViewModel.cs](../Modules/Settings/SettingsViewModel.cs) | [Modules/Settings/SettingsView.xaml](../Modules/Settings/SettingsView.xaml) |
| **Updates** | [Modules/Updates/UpdateViewModel.cs](../Modules/Updates/UpdateViewModel.cs) | [Modules/Updates/UpdateView.xaml](../Modules/Updates/UpdateView.xaml) |

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


