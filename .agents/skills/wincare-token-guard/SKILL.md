---
name: wincare-token-guard
description: >-
  AI context window and token optimization runbook for WinCare Pro. Enforces targeted grep_search and view_file slicing (30-60 lines), zero write_to_file overwrites of large files, minimal diff anchors, and quiet terminal outputs. Use when planning refactorings or file edits across large codebases.
---

# 🧠 WinCare Pro — Token Economy & Surgical Precision Standards (Token Guard)

This playbook establishes operational protocols for AI Agents to execute tasks with minimal token consumption and surgical precision across the WinCare Pro Suite codebase.

---

## 🎯 1. Three-Step Context Economy Protocol

### Step 1: Pinpoint Symbol Locations Without Bulk File Ingestion
- **NEVER** call `view_file` on entire files exceeding 150 lines (e.g., `MainWindow.xaml.cs`, `App.xaml.cs`, `DbManager.cs`).
- **ALWAYS** execute `grep_search` with targeted `Includes: ["*.cs"]` or `["*.xaml"]` filters to locate exact symbol line ranges.
- Follow up with `view_file` targeting a narrow 30-50 line slice around the identified symbol:
  ```json
  { "AbsolutePath": "d:/WinCare/...", "StartLine": 120, "EndLine": 160 }
  ```

### Step 2: Surgical In-Place Patching (Minimal Anchors)
- **STRICTLY FORBIDDEN** to invoke `write_to_file` with `Overwrite = true` on existing source files.
- **MANDATORY** to use `replace_file_content` or `multi_replace_file_content`.
- Retain compact context anchors (`TargetContent`) consisting of 2-4 lines sufficient for unique targeting; do not anchor large blocks when modifying small line subsets.

### Step 3: Terminal Output Noise Suppression
- Suppress default CLI execution verbosity that pollutes context windows with redundant logging:
  ```powershell
  # ❌ Token-Wasting Default Verbosity
  dotnet test
  dotnet build
  
  # ✅ Token-Efficient Minimal Output
  dotnet build WinCarePro.csproj -c Debug -v q --nologo
  dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~TestClass" --logger "console;verbosity=minimal"
  ```

---

## 🚫 2. AI Operational Redlist & Anti-Patterns

1. **API Hallucinations:** Fabricating method overloads or parameters in `SafePathGuard`, `SafeRegistryGuard`, or `ProcessRunner`. Verify canonical signatures in [docs/00_AI_SYMBOL_INDEX.md](../../docs/00_AI_SYMBOL_INDEX.md) prior to invocation.
2. **Silent Catching:** Omitting catch logic or using empty blocks `catch { }`. ALL exceptions MUST be recorded via `CrashLogger.LogError(ex)` and returned as `OperationResult.Fail(ex)`.
3. **Omitting State Cleanup in `finally`:** Failing to clear `IsBusy = false` inside `finally` blocks, resulting in permanently locked UI command bindings on error.
4. **Chat Bloat & Redundant Code Echoing:** Avoid echoing entire files or large code blocks into user responses when targeted diffs suffice. Provide concise summaries covering rationale, target locations, and test outcomes.
