# 🤖 WinCare Pro Suite — AI Agent Operational Directive

> **Version:** v5.0.0 (Codename: Polaris) | **Target Platform:** Windows 10/11 x64  
> **Core Stack:** .NET 10.0 • C# 13 • Windows App SDK (WinUI 3) • SQLite 3 WAL • CommunityToolkit.Mvvm  
> **Deep Reference Docs:** [AI Symbol Index & Architecture Map](file:///d:/WinCare/docs/00_AI_SYMBOL_INDEX.md) • [11 Technical Rulebooks](file:///d:/WinCare/.agents/rules/README.md) • [11 Technical Architecture Chapters](file:///d:/WinCare/docs/README.md) • [10 Specialized Skills](file:///d:/WinCare/.agents/skills/README.md)

---

## 🧭 1. Absolute Priority Hierarchy

$$\mathbf{Safety} > \mathbf{Correctness} > \mathbf{Security} > \mathbf{Stability} > \mathbf{Performance} > \mathbf{Maintainability} > \mathbf{UX} > \mathbf{Aesthetics}$$

1. **Safety:** NEVER corrupt OS files, NEVER delete user personal data, NEVER terminate critical system services.
2. **Correctness:** Telemetry, capacity metrics, and diagnostic data MUST be exact; NEVER fabricate or mock data.
3. **Security:** Zero-Trust. NEVER concatenate CLI commands. ALWAYS isolate arguments via `ArgumentList`. ALWAYS encrypt sensitive data with DPAPI.
4. **Stability:** Zero-Crash. ALWAYS wrap UI mutations in `RunOnUI()`. ALWAYS handle I/O and Win32 faults. ALWAYS propagate `CancellationToken`.
5. **Performance:** Background RAM $\le 15\text{ MB}$, Foreground RAM $\le 85\text{ MB}$, Cold Start $\le 350\text{ ms}$, CPU Idle $\le 0.3\%$.
6. **Maintainability & UX:** Strict MVVM segregation, idiomatic C# 13, Fluent 2.0 Aura Glass UI.

---

## 🏛️ 2. 4-Tier Modular Architecture

Unidirectional dependency branch from exterior to core:  
$$\text{Presentation (Views/XAML)} \longrightarrow \text{ViewModel} \longrightarrow \text{Engines (Business Logic)} \longrightarrow \text{Infrastructure / Core}$$

- **Presentation:** Interacts exclusively with ViewModels via Data Binding & `RelayCommand`. MUST NOT reference Engine or Infrastructure layers directly.
- **ViewModel:** Inherits [ViewModelBase.cs](file:///d:/WinCare/Core/Helpers/ViewModelBase.cs). ALWAYS wrap UI updates in `RunOnUI()`. Cross-VM messaging MUST use `WeakReferenceMessenger`. MUST NOT hold direct references to other ViewModels.
- **Engines:** Isolated business logic. MUST NOT reference any WinUI types (`Button`, `Window`, `SolidColorBrush`, etc.).
- **Infrastructure / Core:** SQLite persistence [DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs), file/registry guards ([SafePathGuard.cs](file:///d:/WinCare/Core/Helpers/SafePathGuard.cs), [SafeRegistryGuard.cs](file:///d:/WinCare/Core/Helpers/SafeRegistryGuard.cs)), P/Invoke (`[LibraryImport]`), DPAPI encryption.

---

## 📋 3. Eleven Inviolable Technical Rules

1. **Architecture & DI:** Enforce View $\rightarrow$ ViewModel $\rightarrow$ Engine $\rightarrow$ Core. Register Singletons (`DbManager`, `ThemeManager`, `TranslationManager`) and Transients (`*Engine`, `*ViewModel`) in [App.xaml.cs](file:///d:/WinCare/App.xaml.cs).
2. **File & Registry Armor:**
   - File/Directory Deletion: MUST pass `SafePathGuard.IsSafeToDelete(path)`. NEVER delete root directories, system trees, user data, or Reparse Points.
   - Registry Mutation/Deletion: MUST pass `SafeRegistryGuard.IsSafeToDeleteKey(key)` and ALWAYS create a restore point first via `RegistryBackupEngine.BackupKey()`. Root hives are STRICTLY FORBIDDEN; key path depth MUST be $\ge 3$.
3. **CLI Safety (Zero Command Injection):** NEVER concatenate shell commands via `cmd.exe /c "..."`. ALWAYS execute via [ProcessRunner.cs](file:///d:/WinCare/Core/Helpers/ProcessRunner.cs) passing parameters through `ArgumentList`. ALWAYS sanitize inputs using [InputSanitizer.cs](file:///d:/WinCare/Core/Helpers/InputSanitizer.cs).
4. **Windows Service Protection & DPAPI:** NEVER stop or disable protected core services (`RpcSs`, `DcomLaunch`, `WinDefend`, `CryptSvc`, etc.). ALWAYS verify via `ServiceSafetyService.IsProtectedService()`. ALWAYS encrypt credentials/secrets using [CryptoHelper.cs](file:///d:/WinCare/Core/Helpers/CryptoHelper.cs) (DPAPI).
5. **WinUI 3 Concurrency & Threading:** UI and `ObservableCollection` mutations MUST be dispatched via `RunOnUI(() => { ... })`. NEVER use sync-over-async (`.Result`, `.Wait()`). NEVER declare `async void` except in UI event handlers.
6. **Task Lifecycle & Cancellation:** Heavy async routines MUST accept `CancellationToken ct = default` and periodically invoke `ThrowIfCancellationRequested()`. ALWAYS ensure state cleanup inside `finally { IsBusy = false; OperationState = OperationState.Idle; }`.
7. **SQLite WAL Concurrency:** ALL SQLite read/write operations in [DbManager.cs](file:///d:/WinCare/Infrastructure/Database/DbManager.cs) MUST be guarded with `lock (_dbLock)`. MUST NOT invoke external I/O or long-running logic inside the lock.
8. **Result Pattern & Zero-Silent Catch:** Fallible methods MUST return `OperationResult` or `OperationResult<T>`. NEVER swallow exceptions with empty `catch { }`. Unexpected errors MUST be recorded to [CrashLogger.cs](file:///d:/WinCare/Infrastructure/Logging/CrashLogger.cs).
9. **Mandatory Internationalization (i18n):** Hardcoded UI strings are STRICTLY FORBIDDEN. ALL display text MUST be registered in both `vi-VN` and `en-US` dictionaries in [TranslationManager.Translations.cs](file:///d:/WinCare/Services/TranslationService/TranslationManager.Translations.cs).
10. **Resource & Memory Discipline:** ALWAYS detach event handlers (`-=`) during `Unloaded`/`Dispose()`. MUST pause 3D/canvas animations when navigating away. Cache shell icons via `IconCacheService`. ALWAYS set `Typography.NumeralAlignment="Tabular"` on telemetry metrics.
11. **Token Economy & Targeted Precision:**
    - **Context Economy & Log Suppression:**
      - Pinpoint symbols via `grep_search` (`Includes: ["*.cs"]`). Read narrow slices (30-60 lines around symbol) via `view_file`. NEVER read entire large files (> 150 lines).
      - Suppress noisy terminal output: Run tests with minimal loggers: `dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~ClassName" --logger "console;verbosity=minimal"`. Run syntax builds with quiet verbosity: `dotnet build WinCarePro.csproj -c Debug -v q --nologo`.
    - **In-Place Patching & Minimal Anchors:**
      - NEVER overwrite entire existing source files using `write_to_file (Overwrite = true)`. ALWAYS use `replace_file_content` or `multi_replace_file_content` with concise 2-4 line context anchors.
    - **Zero-Hallucination API Verification:**
      - ALWAYS verify canonical signatures in `docs/00_AI_SYMBOL_INDEX.md` or through `grep_search` before invoking methods. NEVER guess parameters, return types (`OperationResult<T>`), or `CancellationToken` support.
    - **Native Shield Reuse:**
      - ALWAYS leverage existing infrastructure: `ProcessRunner`, `SafePathGuard`, `SafeRegistryGuard`, `CryptoHelper`, `ServiceSafetyService`, `RunOnUI`, and `lock (_dbLock)`. NEVER re-implement OS-level operations.
    - **Concise Reporting:**
      - DO NOT copy-paste full source blocks into chat when diffs suffice. Provide focused rationale, modified locations, and test outcomes.

---

## 🛑 4. Anti-Pattern Reference Matrix

| Scenario | ❌ STRICTLY FORBIDDEN | ✅ MANDATORY PATTERN |
| :--- | :--- | :--- |
| **Shell Invocation** | `Process.Start("cmd.exe", "/c " + arg)` | `ProcessRunner.RunAsync("app.exe", new[] { arg }, ct)` |
| **File Deletion** | `File.Delete(path)` | `if (SafePathGuard.IsSafeToDelete(path)) File.Delete(path);` |
| **Registry Mutation** | `regKey.DeleteSubKeyTree(name)` | `if (SafeRegistryGuard.IsSafeToDeleteKey(k)) ...` (with backup) |
| **UI Updates from Thread** | `Items.Add(item);` | `RunOnUI(() => Items.Add(item));` |
| **Exception Handling** | `catch { }` | `catch (Exception ex) { CrashLogger.LogError(ex); return OperationResult.Fail(ex); }` |
| **Async Synchronization** | `var r = task.Result;` | `var r = await task;` |
| **UI Display Strings** | `<TextBlock Text="Quét rác"/>` | `<TextBlock Text="{Binding Key, Converter={StaticResource TranslationConverter}}"/>` |
| **Busy State Reset** | Unset or omitted on exception | `try { IsBusy = true; ... } finally { IsBusy = false; }` |
| **SQLite Access** | Direct concurrent un-synchronized queries | `lock (_dbLock) { /* query */ }` |
| **Source Inspection** | `view_file` whole file > 150 lines | `grep_search` target $\rightarrow$ `view_file` 30-60 line slice |
| **Code Modification** | `write_to_file (Overwrite = true)` | `replace_file_content` / `multi_replace_file_content` (short anchor) |
| **Test Execution** | `dotnet test` entire 465 suite (verbose) | `dotnet test --filter "..." --logger "console;verbosity=minimal"` |
| **Syntax Verification** | `dotnet build` default verbosity | `dotnet build -c Debug -v q --nologo` |
| **Legacy Symbol Calling** | Guessing signature or parameter overloads | `grep_search` original signature in `Core/` or `Engines/` |

---

## 🛠️ 5. Standard Operating Procedures (SOPs)

### A. New Module / Feature Implementation

1. **Model:** Define DTOs in `Core/Models/` returning `OperationResult<T>`.
2. **Engine:** Author in `Engines/<Category>/`, wrap with native safety guards, accept `CancellationToken ct = default`.
3. **DI:** Register Engine and ViewModel dependencies in `App.xaml.cs`.
4. **ViewModel:** Implement in `Modules/<Feature>/`, inherit `ViewModelBase`, wrap UI bindings in `RunOnUI()`, handle `finally`.
5. **View:** Create XAML with Fluent 2.0 Aura Glass, 8px corner radii, Tabular figures, and `TranslationConverter`.
6. **i18n:** Synchronize identical key pairs across `vi-VN` and `en-US` in `TranslationManager.Translations.cs`.
7. **Test & Verify:** Implement xUnit unit tests in `WinCarePro.Tests/` and validate via `dotnet test`.

### B. Bug Fixing SOP

1. Isolate the target failure tier (`View`, `ViewModel`, `Engine`, or `Core/Infra`).
2. Enforce relevant architectural invariant (Rule 02/03/05/09/11).
3. Execute regression test: `dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~..."`.
4. Add at least one regression test to guarantee zero recurrence.

---

## 🧰 6. Technical Playbooks Directory (Workspace Skills)

Quick lookup before implementation at [skills/README.md](file:///d:/WinCare/.agents/skills/README.md):

| Skill Name | Path | Core Scope & Responsibilities |
| :--- | :--- | :--- |
| `wincare-module-creator` | [wincare-module-creator/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-module-creator/SKILL.md) | Standardized 7-step modular workflow (`Model` $\rightarrow$ `Engine` $\rightarrow$ `DI` $\rightarrow$ `ViewModel` $\rightarrow$ `Aura` $\rightarrow$ `i18n` $\rightarrow$ `Test`). |
| `wincare-safe-ops` | [wincare-safe-ops/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-safe-ops/SKILL.md) | Zero-Trust OS operations: `SafePathGuard`, `SafeRegistryGuard`, registry backups, `ProcessRunner`, `ServiceSafetyService`, DPAPI. |
| `wincare-concurrency-guard` | [wincare-concurrency-guard/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-concurrency-guard/SKILL.md) | WinUI 3 threading: `RunOnUI()`, `DispatcherQueue`, RelayCommands, `CancellationToken`, eliminating sync-over-async. |
| `wincare-ui-aura` | [wincare-ui-aura/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-ui-aura/SKILL.md) | Aura Glass Fluent 2.0 UI: Tabular figures, Mica Alt, unsubscribe `Unloaded` events, zero memory leaks. |
| `wincare-i18n-sync` | [wincare-i18n-sync/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-i18n-sync/SKILL.md) | Dual-language synchronization: `vi-VN` & `en-US` in `TranslationManager.Translations.cs`, `TranslationConverter`, zero hardcoded text. |
| `wincare-db-wal` | [wincare-db-wal/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-db-wal/SKILL.md) | SQLite WAL management: centralized `lock (_dbLock)`, parameterized SQL, `DatabaseTestCollection` for xUnit. |
| `wincare-error-resilience` | [wincare-error-resilience/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-error-resilience/SKILL.md) | Resilience: `OperationResult<T>` pattern, Zero-Silent Catch, `CrashLogger.LogError`, graceful fallback. |
| `wincare-test-verifier` | [wincare-test-verifier/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-test-verifier/SKILL.md) | Token-efficient testing: filtered test runs (`--filter`), regression analysis, target 100% Passed. |
| `wincare-token-guard` | [wincare-token-guard/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-token-guard/SKILL.md) | Token optimization: `grep_search` targeted lookups, narrow 30-60 line slicing, in-place patching, quiet logs. |
| `wincare-ci-packaging` | [wincare-ci-packaging/SKILL.md](file:///d:/WinCare/.agents/skills/wincare-ci-packaging/SKILL.md) | CI & packaging: `publish.bat` (Self-Contained x64 .NET 10), `publish_installer.bat` (Inno Setup), SSOT `AppConstants`. |

---

## ⚡ 7. Operational Commands & Completion Checklist

```powershell
dotnet build WinCarePro.csproj -c Debug                         # Syntax and compilation check
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --verbosity normal # Run full test suite (Target: 100% Passed - 465 Tests)
.\publish.bat                                                  # Self-Contained x64 deployment
.\publish_installer.bat                                        # Build Inno Setup installer package
```

### ✅ Pre-Handoff Quality Checklist:

- [ ] **1. Priority Order:** Enforce `Safety > Correctness > Security > Stability > Performance`.
- [ ] **2. Clean Architecture:** View $\rightarrow$ ViewModel $\rightarrow$ Engine $\rightarrow$ Core; zero WinUI controls inside Engines.
- [ ] **3. Zero-Trust Safety:** Use `ProcessRunner`, `SafePathGuard`, `SafeRegistryGuard`, DPAPI; zero command injection.
- [ ] **4. Zero-Crash Concurrency:** 100% UI mutations marshaled through `RunOnUI()`; no sync-over-async deadlocks.
- [ ] **5. Cancellation & Finally:** ALWAYS clear `IsBusy` and state flags in `finally` blocks.
- [ ] **6. SQLite WAL Safe:** ALL database queries wrapped in `lock (_dbLock)`.
- [ ] **7. Zero-Silent Catch:** No empty `catch` blocks; return `OperationResult` or log via `CrashLogger`.
- [ ] **8. Full i18n:** Synchronized key pairs in both `vi-VN` and `en-US` dictionaries.
- [ ] **9. Clean Memory:** Detach XAML event handlers, cleanup visual animations, dispose unmanaged resources.
- [ ] **10. 100% Unit Tests Passed:** All 465 xUnit test cases pass (`0 Failed`); no new compiler warnings.
- [ ] **11. Token Economy & Zero-Overwrite:** Localized diff/patching; targeted narrow line reads; reuse Core Guards.
- [ ] **12. Native Interop:** MUST NOT use `[DllImport]`; all Win32 APIs MUST use source-generated `[LibraryImport]`.
- [ ] **13. Test Suite Integrity:** NEVER weaken, mock away, or delete existing test cases in `WinCarePro.Tests/`.
- [ ] **14. Clean Source Tree:** Clean `git status`; zero orphaned `.tmp`, `.orig`, or scratch files.
