# 🔒 02. Security & Safety Rules

> [⬅️ 01. Architecture & Design](01_ARCHITECTURE_AND_DESIGN_RULES.md) • [🏠 Rules Index](README.md) • **Rule 02** • [Next Rule: 03. Concurrency & Threading ➡️](03_CONCURRENCY_AND_THREADING_RULES.md)

---

## 🚫 1. Command Injection Prevention (`ProcessRunner` Invariants)

WinCare Pro executes native OS CLI utilities (`winget.exe`, `sfc.exe`, `dism.exe`, `reg.exe`, `powercfg.exe`, `netsh.exe`).

### 🔴 MANDATORY INVARIANTS:
1. **NEVER** use raw string concatenation to compose executable command lines.
2. **ALWAYS** execute commands via [ProcessRunner.cs](../../Core/Helpers/ProcessRunner.cs) and pass parameters isolated via `ProcessStartInfo.ArgumentList`.
3. All external user input and third-party package IDs MUST be sanitized via [InputSanitizer.cs](../../Core/Helpers/InputSanitizer.cs).

### ❌ FORBIDDEN Code (Critical Injection Vulnerability):
```csharp
// VULNERABLE: Susceptible to command chaining, e.g. "app & shutdown -s -t 0"
var process = Process.Start("cmd.exe", "/c winget upgrade " + userAppId);
```

### ✅ Production-Grade Mandatory Pattern:
```csharp
var psi = new ProcessStartInfo
{
    FileName = "winget.exe",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};
// Every argument is isolated in the argument list:
psi.ArgumentList.Add("upgrade");
psi.ArgumentList.Add("--id");
psi.ArgumentList.Add(InputSanitizer.Sanitize(userAppId));
psi.ArgumentList.Add("--silent");
```

---

## 🛡️ 2. System File & Data Loss Prevention (`SafePathGuard`)

All file and directory deletion operations across the entire application (specifically within `JunkCleanerEngine`, `UninstallEngine`, `DiskEngine`) **MUST** invoke `SafePathGuard.IsSafeToDelete(path)` prior to executing `File.Delete()` or `Directory.Delete()`.

### Blacklisted Deletion Targets:
- Drive roots: `C:\`, `D:\`, `E:\`
- Critical system trees: `C:\Windows`, `C:\Windows\System32`, `C:\Windows\SysWOW64`, `C:\Windows\WinSxS`, `C:\Program Files`, `C:\Program Files (x86)`, `C:\ProgramData`
- User profile personal directories: `Desktop`, `Documents`, `Pictures`, `Music`, `Videos`, `Downloads`
- Sensitive credentials & browser state: `Login Data`, `Web Data`, `Cookies`, `Local State`, `SAM`, `SYSTEM`, `SECURITY`

### Non-Recursive Directory Deletion:
- When calling `Directory.Delete(path, recursive: false)`, the directory MUST be verified empty. If subfiles or directories remain, deletion MUST be rejected to avoid uncontrolled data destruction.

### Reparse Point & Symlink Junction Traversal:
- **NEVER traverse Junction Points recursively:** When scanning temporary directories, encountering `FileAttributes.ReparsePoint` MUST bypass deep traversal to prevent junction redirection attacks.

---

## 🧬 3. Core Registry Defense (`SafeRegistryGuard`)

1. **Root Hive Deletion FORBIDDEN:** Strictly forbid deletions targeting root hives: `HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`.
2. **Vital System Key Protection:** Protect system-critical branches including `HKLM\SYSTEM\CurrentControlSet\Control` and `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion`.
3. **Minimum Key Path Depth:** Any registry subkey deletion MUST have a path depth of at least 3 levels (`SegmentCount >= 3`).
4. **Boot Value Protection:** NEVER alter or delete `Shell` (default `explorer.exe`) or `Userinit` (default `userinit.exe`).

---

## 🛡️ 4. Windows Core Service Armor (`ServiceSafetyService`)

1. **Protected Service Whitelist:**
   - NEVER disable or terminate critical OS services: `RpcSs`, `DcomLaunch`, `SamSs`, `gpsvc`, `ProfSvc`, `BFE`, `WinDefend`, `CryptSvc`, `EventLog`, `PlugPlay`, `RpcEptMapper`.
2. **Pre-Mutation Validation:**
   - All optimization and state mutations in `StartupEngine` or `SystemOptimizerEngine` MUST invoke `ServiceSafetyService.IsProtectedService(serviceName)`. Protected services MUST be rejected with a safety warning.

---

## 🔐 5. Digital Signature Verification & Data Encryption

1. **WinVerifyTrust Interop:**
   - Third-party installer packages downloaded by `SoftwareUpdaterEngine` MUST pass Authenticode digital signature validation via the native Win32 `WinVerifyTrust` API prior to silent execution.
2. **Windows DPAPI Data Protection:**
   - Sensitive user configuration items (tokens, API keys, private credentials) persisted in SQLite MUST be encrypted via [CryptoHelper.cs](../../Core/Helpers/CryptoHelper.cs) using `ProtectedData.Protect` with `DataProtectionScope.CurrentUser`.
   - Storing passwords or credentials in plain-text is STRICTLY FORBIDDEN.

---

## 🔄 6. Resilience & State Rollback (Snapshot Before Action)

Prior to executing any registry mutation, service disabling, or system tweak:
1. ALWAYS record the pre-mutation state into the `Snapshots` table via `DbManager.SaveSnapshot()` or generate a `.reg` backup via `RegistryBackupEngine`.
2. ALWAYS provide 1-Click rollback support via `UndoManagerService`.

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>
