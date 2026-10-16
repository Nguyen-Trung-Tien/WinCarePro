# 🧪 07. Testing & Quality Assurance Rules

> [⬅️ 06. UI/UX & Aesthetics](06_UI_UX_AND_AESTHETICS_RULES.md) • [🏠 Rules Index](README.md) • **Rule 07** • [Next Rule: 08. Release Packaging & CI/CD ➡️](08_RELEASE_PACKAGING_AND_CICD_RULES.md)

---

## 🏆 1. Zero-Failure Quality Gate

The project enforces a strict **100% Passed (465/465 Tests)** pass rate. Release candidate builds with any failing tests are STRICTLY REJECTED.

```powershell
# Mandatory pre-merge test execution command:
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --logger "console;verbosity=minimal"
```

---

## 🛡️ 2. Test Isolation & Sandbox Safety

1. **Host Environment Protection:**
   - Database tests **MUST** execute against in-memory databases (`Data Source=:memory:`) or isolated scratch databases in `TestTemp/`, cleaned up automatically during test teardown.
   - Hazardous Win32 calls (process termination, `HKLM` modifications, `sfc /scannow`) MUST be isolated behind mocks or tested against simulated virtual filesystems.
2. **Order-Independent Concurrency:**
   - Tests MUST remain order-independent. Shared mutable static state between concurrent test runners is STRICTLY FORBIDDEN.

---

## 🎯 3. Mandatory Coverage Matrix for New Features

Introducing any new Engine, ViewModel, or system module **REQUIRES** corresponding xUnit test cases in `WinCarePro.Tests`:

| Feature Domain | Minimum Test Scenarios |
| :--- | :--- |
| **Lifecycle & Cancellation** | Verify `CancelScan()` restores `IsBusy == false` and `OperationState.Idle`; verify zero lock contention. |
| **Realistic Stress Scenarios** | Cycle test `Scan → Cancel → Scan`, rapid multi-click execution, mid-scan theme/language transitions. |
| **Scanning & File Operations** | Path classification, skipping locked handles, `SafePathGuard` safety barriers. |
| **Diagnostics & Scoring** | Boundary cases: 0% RAM, 100% CPU, depleted disk storage, empty collection states. |
| **Security & Persistence** | SQL injection resistance, database schema migration integrity, DPAPI round-trip encryption. |
| **Internationalization (i18n)** | Zero missing translation keys between `vi-VN` and `en-US` dictionaries. |

---

## 🔍 4. Canonical Test Pattern (Arrange - Act - Assert)

All test methods MUST adhere to AAA structure and use `FluentAssertions`:

```csharp
[Fact]
public void SafePathGuard_ShouldBlockDeletion_WhenTargetIsSystem32()
{
    // 1. Arrange
    string dangerousPath = @"C:\Windows\System32\drivers";

    // 2. Act
    bool isSafe = SafePathGuard.IsSafeToDelete(dangerousPath);

    // 3. Assert
    isSafe.Should().BeFalse("System32 is a vital operating system tree and must never be deleted.");
}
```

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>
