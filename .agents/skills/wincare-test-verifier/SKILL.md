---
name: wincare-test-verifier
description: >-
  Token-efficient unit testing and regression verification workflow for WinCare Pro. Covers running targeted tests, handling SQLite DatabaseTestCollection fixtures, interpreting test failures, and ensuring 100% test passing before completion. Use when running tests, diagnosing test failures, or validating changes in WinCarePro.Tests.
---

# 🧪 WinCare Pro — Token-Efficient Test Verification (Test Verifier Workflow)

WinCare Pro maintains a comprehensive 465+ xUnit regression test suite. Full unfiltered test runs waste thousands of tokens and pollute context windows.

## 📁 Skill Assets & Helpers
- **Targeted Test Runner:** [`scripts/run-targeted-tests.ps1`](./scripts/run-targeted-tests.ps1)
- **Reference Examples:**
  - Standard xUnit Test Pattern: [`examples/StandardEngineTests.cs`](./examples/StandardEngineTests.cs)
- **Root Cause Analysis (RCA):**
  - Failure Troubleshooting Guide: [`resources/test-failure-rca-guide.md`](./resources/test-failure-rca-guide.md)

---

## ⚡ 1. Efficient Filtered Test Invocations

Always use filtered test runs and minimal verbosity:

```powershell
# Targeted test execution (Saves 95% tokens)
dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --filter "FullyQualifiedName~SecurityAndSafetyTests" --logger "console;verbosity=minimal"
```

---

## 🗄️ 2. Database Isolation Invariant

When authoring or modifying tests that interact with `DbManager`, you **MUST** decorate the test class with:

```csharp
[Collection(nameof(DatabaseTestCollection))]
public class MyDatabaseTests
{
    // Tests will run sequentially with other DB tests, avoiding WAL lock collisions
}
```
