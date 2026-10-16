---
trigger: always_on
---

# 🧠 WinCare Pro — Rule 11: AI Agent Efficiency & Precision Engineering

> **Standard File:** `.agents/rules/11_AI_AGENT_EFFICIENCY_AND_PRECISION_RULES.md`  
> **Target Scope:** All AI Agents (Antigravity/Gemini), automated pair programmers, and core contributors  
> **Core Objective:** Minimize prompt and output token consumption (Context & Output Token Economy), eliminate hallucination (Zero-Hallucination), enforce 100% architectural compliance, and guarantee deterministic engineering precision across WinCare Pro Suite.

---

## 🏛️ 1. Core Directives

When engineering or maintaining code within WinCare Pro, AI Agents **MUST** strictly obey:

$$\mathbf{Precision\ Engineering} + \mathbf{Token\ Economy} \Longrightarrow \mathbf{Zero\ Downtime} + \mathbf{Optimal\ Cost}$$

1. **Zero Context Bloat:** Inspect only the precise slice of code required. NEVER load full multithousand-line files into the context window without specific necessity.
2. **Zero-Overwrite Invariant:** Patch only target code blocks via localized diffs. NEVER rewrite entire source files.
3. **Native Shield Reuse:** Leverage established WinCare Pro Guards, Helpers, and Services. NEVER re-invent external logic or native primitives.
4. **Self-Correction Verification Gate:** Every code change MUST be validated with zero compiler warnings and a 100% passing rate across all regression unit tests.

---

## 📉 2. Token Economy Protocols

```mermaid
graph TD
    classDef step fill:#0f172a,stroke:#3b82f6,stroke-width:2px,color:#fff;
    classDef bad fill:#450a0a,stroke:#ef4444,stroke-width:2px,color:#fff;
    classDef good fill:#064e3b,stroke:#10b981,stroke-width:2px,color:#fff;

    Start["Source Modification Request"]:::step
    Search["Step 1: grep_search targets symbol / error line"]:::step
    Start --> Search

    CheckFile{"File > 150 lines?"}:::step
    Search --> CheckFile

    CheckFile -- Yes --> Slice["✅ Use view_file with StartLine/EndLine (30-60 lines)"]:::good
    CheckFile -- No --> ViewAll["View complete compact file"]:::good

    Edit["Step 2: Apply code modifications"]:::step
    Slice --> Edit
    ViewAll --> Edit

    BadEdit["❌ FORBIDDEN: write_to_file (Overwrite = true) full file"]:::bad
    GoodEdit["✅ MANDATORY: replace_file_content / multi_replace_file_content"]:::good
    Edit -.-> BadEdit
    Edit --> GoodEdit

    Verify["Step 3: Verification Gate"]:::step
    GoodEdit --> Verify

    BadTest["❌ FORBIDDEN: Running full 465 test suite for small diff"]:::bad
    GoodTest["✅ MANDATORY: dotnet test --filter \"ClassName\" for target module"]:::good
    Verify -.-> BadTest
    Verify --> GoodTest
```

### 2.1 Targeted Slicing Over File Dumps
- **FORBIDDEN:** Calling `view_file` on large files (e.g. `App.xaml.cs`, `MainWindow.xaml.cs`, `DbManager.cs`), which squanders thousands of prompt tokens and dilutes attention.
- **MANDATORY:**
  1. Pinpoint targets using `grep_search` with file filters `Includes: ["*.cs"]` or `["*.xaml"]` to locate exact method names, variables, or error diagnostics.
  2. Use `view_file` with narrow slices (`StartLine` and `EndLine`), inspecting strictly 30 to 60 lines around the target symbol.
  3. NEVER re-read files already present in recent context unless modified.

### 2.2 Minimal-Anchor In-Place Patching
- **FORBIDDEN:** Invoking `write_to_file` with `Overwrite = true` on existing codebase files. Full overwrites:
  - Waste excessive output tokens (2,000–8,000 tokens per call).
  - Risk dropping helper methods, `using` directives, or corrupting indentation.
  - Generate massive churn and noise in Git diff history.
- **MANDATORY:**
  - Use `replace_file_content` for single contiguous edits.
  - Use `multi_replace_file_content` for multiple non-contiguous edits within the same file.
  - **Minimal-Anchor Rule:** In `TargetContent`, supply strictly 2 to 4 lines of preceding and following context to create a unique match key. NEVER copy 30–50 lines of unmodified code into anchors.

### 2.3 Terminal Output Suppression & Quiet Flags
- **FORBIDDEN:** Running default unfiltered `dotnet test` or `dotnet build` commands. Unfiltered tests dump 400+ lines of verbose logs, exhausting token budgets.
- **MANDATORY:**
  - **Selective Testing with Minimal Logger:**
    ```powershell
    # ✅ Saves 95% tokens: outputs only summary and failures
    dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~ProcessRunnerTests" --logger "console;verbosity=minimal"
    dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~SecurityAndSafetyTests" --logger "console;verbosity=minimal"
    ```
  - **Quiet Compilation Verification:**
    ```powershell
    # ✅ Saves 100% extraneous tokens: outputs solely upon syntax/build failure
    dotnet build WinCarePro.csproj -c Debug -v q --nologo
    # Or restrict to errors only:
    dotnet build WinCarePro.csproj -c Debug -clp:ErrorsOnly --nologo
    ```
  - **Compact Git Commands:** Use `git status -s` instead of `git status`; use `git log -n 3 --oneline` instead of `git log`.

| Command Invocation | ❌ Wasteful Pattern (3,000+ tokens) | ✅ Compact Pattern (< 200 tokens) |
| :--- | :--- | :--- |
| **Unit Testing** | `dotnet test` (Full suite, verbose) | `dotnet test --filter "FullyQualifiedName~..." --logger "console;verbosity=minimal"` |
| **Build Verification** | `dotnet build` (Prints SDK and targets header) | `dotnet build -c Debug -v q --nologo` |
| **Git Status** | `git status` (Prints branch guides & help text) | `git status -s` |

### 2.4 Concise Engineering Communication
- NEVER reproduce full source blocks in explanations when tool diffs already communicate changes.
- Structure responses strictly in 3 parts:
  1. **Root Cause:** Concise 1-2 sentence diagnosis.
  2. **Intervention Location:** Markdown links with line anchors (e.g. [`SafePathGuard.cs:L45-L60`](../../Core/Helpers/SafePathGuard.cs#L45-L60)).
  3. **Verification Outcome:** Specific test metrics (Passed count, execution duration).

---

## 🎯 3. Precision Engineering Protocols for WinCare Pro

To guarantee zero regression and zero OS disruption, AI Agents MUST strictly enforce:

### 3.1 Strict 4-Tier Boundary Enforcement
```
Presentation (Views/XAML) ──> ViewModels ──> Engines ──> Infrastructure/Core
```
- **Zero WinUI Leaks into Lower Tiers:** `using Microsoft.UI.Xaml;`, `Button`, `SolidColorBrush`, `Window` are STRICTLY FORBIDDEN inside `Engines/`, `Core/`, and `Infrastructure/`.
- **ViewModel Independence:** Inherit from [ViewModelBase.cs](../../Core/Helpers/ViewModelBase.cs), marshal all UI updates through `RunOnUI()`, communicate via `WeakReferenceMessenger`, and never retain references to Pages or Windows.

### 3.2 Zero-Hallucination API Verification Protocol
- **FORBIDDEN:** Assuming method identifiers, parameter layouts, or return types of internal services.
- **MANDATORY:** Before invoking any Engine, Service, or Helper, verify its canonical declaration via `grep_search` in `Core/` or `Engines/`:
  1. **Constructor & DI:** Verify parameter sequences required for registration.
  2. **Return Type:** Confirm whether operations return `OperationResult` or `OperationResult<T>`.
  3. **Cancellation Support:** Verify support for `CancellationToken ct = default` and propagate it through async I/O.
  4. **Resource Teardown:** Confirm if the type implements `IDisposable` to ensure proper `using` or event unsubscription.

### 3.3 Mandatory Reuse of Core Guards
Before writing any OS-level operations, the Agent MUST leverage established infrastructure guards:

| Critical Operation | Mandatory Guard Class | Canonical Implementation Pattern |
| :--- | :--- | :--- |
| **CLI / Process Invocation** | [ProcessRunner.cs](../../Core/Helpers/ProcessRunner.cs) | `await ProcessRunner.RunAsync("dism.exe", new[] { "/Online", "/Cleanup-Image", "/ScanHealth" }, ct);` |
| **CLI Input Sanitization** | [InputSanitizer.cs](../../Core/Helpers/InputSanitizer.cs) | `var cleanArg = InputSanitizer.Sanitize(rawInput);` |
| **File / Directory Deletion** | [SafePathGuard.cs](../../Core/Helpers/SafePathGuard.cs) | `if (SafePathGuard.IsSafeToDelete(targetPath)) { File.Delete(targetPath); }` |
| **Registry Mutation / Deletion** | [SafeRegistryGuard.cs](../../Core/Helpers/SafeRegistryGuard.cs) & `RegistryBackupEngine` | `if (SafeRegistryGuard.IsSafeToDeleteKey(regKey)) { await RegistryBackupEngine.BackupKeyAsync(regKey); ... }` |
| **Windows Service Control** | `ServiceSafetyService` | `if (!ServiceSafetyService.IsProtectedService(svcName)) { /* Stop or disable service */ }` |
| **Sensitive Data Encryption** | [CryptoHelper.cs](../../Core/Helpers/CryptoHelper.cs) | `var encrypted = CryptoHelper.Protect(plainText);` |
| **UI State Mutation** | `ViewModelBase.RunOnUI()` | `RunOnUI(() => { Items.Add(newItem); StatusText = "Complete"; });` |
| **SQLite WAL Queries** | [DbManager.cs](../../Infrastructure/Database/DbManager.cs) | `lock (_dbLock) { /* Execute parameterized SQLite query */ }` |

### 3.4 Layer-by-Layer Verification Pipeline
To prevent cascading compile failures:
1. **Model / DTO:** Declare models in `Core/Models/`. Run `dotnet build WinCarePro.csproj -c Debug -v q --nologo` to verify syntax.
2. **Engine:** Author business logic in `Engines/`, encapsulate with safety guards, accept `CancellationToken ct = default`.
3. **Engine Unit Test:** Author tests in `WinCarePro.Tests/` and run filtered test targets:
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TargetEngineTests" --logger "console;verbosity=minimal"
   ```
4. **DI Registration:** Register Engine and ViewModel dependencies in [App.xaml.cs](../../App.xaml.cs).
5. **ViewModel:** Author in `Modules/`, inherit `ViewModelBase`, wrap UI updates in `RunOnUI()`, handle `try/finally` for `IsBusy`.
6. **XAML View:** Author interface with Aura Glass styling, 8px corner radii, Tabular figures, and `TranslationConverter`.
7. **i18n Synchronization:** Register key pairs across both `vi-VN` and `en-US` in [TranslationManager.Translations.cs](../../Services/TranslationService/TranslationManager.Translations.cs).
8. **Comprehensive Validation Gate:** Execute the full 465-test suite once prior to handoff.

### 3.5 Zero-Silent Catch & State Hygiene
1. **Silent Catches Strictly FORBIDDEN:**
   ```csharp
   // ❌ STRICTLY FORBIDDEN
   catch { }

   // ✅ MANDATORY PRODUCTION PATTERN
   catch (Exception ex)
   {
       CrashLogger.LogError(ex);
       return OperationResult<bool>.Fail($"Operation failed: {ex.Message}");
   }
   ```

2. **Mandatory State Reset in `finally`:**
   ```csharp
   try
   {
       IsBusy = true;
       OperationState = OperationState.Running;
       // Business workflow...
   }
   finally
   {
       IsBusy = false;
       OperationState = OperationState.Idle;
   }
   ```

### 3.6 Complete Dual-Language i18n Parity
- **Zero Hardcoded Strings:** All user-facing strings MUST bind via `TranslationConverter` or resolve through `TranslationManager.Instance.GetString(key)`.
- When introducing new features with UI strings, ALWAYS register matching keys in both `vi-VN` and `en-US` in [TranslationManager.Translations.cs](../../Services/TranslationService/TranslationManager.Translations.cs).

---

## ⚡ 4. Standard Operating Procedure for Agents (AI SOP)

When handling coding tasks:

```powershell
# Step 1: Rapid symbol lookup via filtered ripgrep (saves prompt tokens)
grep_search(Query: "TargetMethod", SearchPath: "d:\WinCare", Includes: ["*.cs"])

# Step 2: Read narrow source slice (30-60 lines)
view_file(AbsolutePath: "...", StartLine: 120, EndLine: 165)

# Step 3: Apply minimal-anchor diff patch (saves output tokens)
replace_file_content(...)

# Step 4: Targeted unit test execution with minimal verbosity
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TargetModule" --logger "console;verbosity=minimal"

# Step 5: Verification Gate prior to handoff
dotnet build WinCarePro.csproj -c Debug -v q --nologo            # MUST yield 0 Errors, 0 Warnings
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj            # MUST achieve 100% Passed (465/465 Tests)
```

---

## 🚫 5. AI Anti-Pattern Reference Matrix

| AI Anti-Pattern | ❌ Negative Impact | ✅ Standardized Mandatory Pattern |
| :--- | :--- | :--- |
| **Viewing entire 500-1000 line file** | Context window bloat, token exhaustion, degraded reasoning. | Use `grep_search` $\rightarrow$ `view_file` targeted 30-60 line slices. |
| **Full file rewrite via `write_to_file`** | Massive output token consumption, lost methods, indentation corruption. | Use `replace_file_content` / `multi_replace_file_content` with short anchors. |
| **Generating `Process.Start("cmd.exe", ...)`** | Command Injection vulnerability, violates Rule 02. | Use `ProcessRunner.RunAsync("...", args, ct)` with `ArgumentList`. |
| **Running default `dotnet test` repeatedly** | 400+ lines of verbose logs, exhausting token context. | Use `--filter` + `--logger "console;verbosity=minimal"`. |
| **Running default verbose `dotnet build`** | Dumps dozens of redundant compiler headers. | Use `dotnet build -c Debug -v q --nologo` or `-clp:ErrorsOnly`. |
| **Hallucinating API signatures** | Build errors and runtime exceptions. | Verify declarations in `Core/` or `Engines/` via `grep_search`. |
| **Hardcoding Vietnamese strings in XAML** | Breaks runtime dynamic English/Vietnamese toggling. | Declare keys in `TranslationManager.Translations.cs` and bind via `TranslationConverter`. |
| **Omitting `finally` block for `IsBusy`** | UI hangs permanently in loading state on unhandled exception. | ALWAYS reset `IsBusy = false` inside `finally` blocks. |

---

<div align="center">
  <sub>Rule 11 guarantees deterministic performance, safety, and token efficiency within <b>WinCare Pro Suite</b></sub>
</div>
