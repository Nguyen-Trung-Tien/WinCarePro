---
name: wincare-test-verifier
description: >-
  Token-efficient unit testing and regression verification workflow for WinCare Pro. Covers running targeted tests, handling SQLite DatabaseTestCollection fixtures, interpreting test failures, and ensuring 100% test passing before completion. Use when running tests, diagnosing test failures, or validating changes in WinCarePro.Tests.
---

# 🧪 WinCare Pro — Regression Verification & Testing Standards (Test Verifier)

WinCare Pro contains **465 comprehensive xUnit unit tests** protecting system integrity. This guide details canonical testing commands, token conservation techniques, and safe SQLite concurrency rules.

---

## ⚡ 1. Token-Efficient Targeted Test Execution

### ❌ Anti-Patterns to Avoid:
- Running `dotnet test` across the entire 465-test suite repeatedly after every minor modification. This emits hundreds of lines of redundant terminal output, burning 5,000 to 15,000 unnecessary tokens.

### ✅ Mandatory AI Workflow:
1. **Target execution exclusively to the active test class:**
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~ClassName" --logger "console;verbosity=minimal"
   ```
   *Example:*
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~JunkCleanerEngineTests" --logger "console;verbosity=minimal"
   ```
2. **Execute rapid syntax compilation checks prior to running tests:**
   ```powershell
   dotnet build WinCarePro.csproj -c Debug -v q --nologo
   ```
3. **Execute full test suite verification ONLY upon completing feature scope (Final Smoke Test):**
   ```powershell
   dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj -c Debug --logger "console;verbosity=minimal"
   ```

---

## 🗄️ 2. SQLite Test Concurrency Standards (`DatabaseTestCollection`)

Because SQLite operates in Single-Writer WAL mode with centralized `lock (_dbLock)`, unconstrained parallel test execution across multiple classes accessing SQLite will cause `database is locked` contention errors or data race conditions.

### Mandatory Rule:
All test classes invoking `DbManager` or business engines recording Audit/History logs **MUST** apply the collection attribute:
```csharp
[Collection(nameof(DatabaseTestCollection))]
public class MyFeatureDbTests : IClassFixture<MyFixture>
{
    // ...
}
```
This attribute instructs xUnit to serialize database test executions sequentially, preventing race conditions and resource collisions.

---

## 🔍 3. Root Cause Analysis (RCA) Protocol on Test Failure

1. **Examine the exact diagnostic failure message:**
   - Review expected vs. actual Assert values.
   - Inspect the stack trace identifying the failing line number.
2. **Eliminate speculation:**
   - Execute `grep_search` to verify canonical signatures and referenced constant values.
   - Execute `view_file` on a narrow 30-50 line slice around the failure site.
3. **Apply surgical in-place patching:**
   - Use `replace_file_content` to resolve the root cause.
   - Re-run the isolated failing test:
     ```powershell
     dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~ClassName.TestMethod" --logger "console;verbosity=minimal"
     ```
4. **Handoff Quality Gate:**
   - 100% Passed (465/465 Tests), 0 Failed, 0 Skipped.
