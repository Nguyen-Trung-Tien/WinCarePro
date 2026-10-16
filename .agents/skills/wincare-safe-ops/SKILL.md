---
name: wincare-safe-ops
description: >-
  Mandatory safety procedures and rules for OS operations in WinCare Pro, including file deletions (SafePathGuard), registry edits/deletions (SafeRegistryGuard and backup), CLI execution (ProcessRunner with ArgumentList), service protection (ServiceSafetyService), and DPAPI encryption (CryptoHelper). Use whenever writing code that touches files, registry, CLI processes, or Windows services.
---

# 🛡️ WinCare Pro — Safe Operating System Operations Standards (Safe OS Operations)

WinCare Pro executes deep system modifications across Windows 10/11 x64. All AI Agents and developers **MUST** apply Zero-Trust Safeguards prior to modifying files, registry entries, processes, and Windows services.

---

## 🚫 1. Safe Operations Enforcement Matrix

| Operation Category | ❌ STRICTLY FORBIDDEN | ✅ MANDATORY PATTERN |
| :--- | :--- | :--- |
| **File/Directory Deletion** | Unchecked `File.Delete(p)`, `Directory.Delete(p)` | `if (SafePathGuard.IsSafeToDelete(p)) { File.Delete(p); }` |
| **Registry Mutation** | `regKey.DeleteSubKeyTree(k)` | Verify `SafeRegistryGuard.IsSafeToDeleteKey(root, subKey)` & backup beforehand via `RegistryBackupEngine.BackupKey(...)` |
| **CLI Execution** | `Process.Start("cmd.exe", "/c " + cmd)` | `ProcessRunner.RunAsync("app.exe", new[] { arg1, arg2 }, ct)` |
| **Service Modification** | Direct calls to `sc stop <name>` | Verify `ServiceSafetyService.IsProtectedService(name)` prior to stopping |
| **Sensitive Data** | Storing plaintext credentials/tokens/keys | Encrypt via `CryptoHelper.Protect(plainText)` using Windows DPAPI |
| **SQLite Access** | Direct concurrent multi-threaded read/write | ALWAYS synchronize via `lock (_dbLock)` in `DbManager.cs` |

---

## 📁 2. File Safety Standards (`SafePathGuard`)

Prior to deleting any file or directory:
1. Verify safety:
   ```csharp
   if (!SafePathGuard.IsSafeToDelete(targetPath))
   {
       AppLogger.LogWarning($"[SafePathGuard] Blocked deletion attempt of protected path: {targetPath}");
       return OperationResult.Fail($"Path '{targetPath}' is protected by SafePathGuard.");
   }
   ```
2. `SafePathGuard` automatically rejects:
   - Root volumes (`C:\`, `D:\`, ...).
   - Core Windows directories (`C:\Windows`, `System32`, `SysWOW64`, `WinSxS`).
   - Critical user personal data (`Desktop`, `Documents`, `Pictures`, `Music`, `Videos`, `Downloads`).
   - System installation folders (`Program Files`, `ProgramData`).
   - Reparse Points / Junctions / Symbolic links (eliminating Directory Traversal attacks via symlink escalation).

---

## 🗝️ 3. Registry Safety Standards (`SafeRegistryGuard`)

1. **Pre-mutation backup requirement:**
   ```csharp
   var backupResult = await _backupEngine.BackupKeyAsync(rootHive, subKeyPath, ct);
   if (!backupResult.Success)
   {
       return OperationResult.Fail("Registry backup failed. Operation aborted for safety.");
   }
   ```
2. **Hierarchy depth and whitelist validation:**
   ```csharp
   if (!SafeRegistryGuard.IsSafeToDeleteKey(rootHive, subKeyPath))
   {
       return OperationResult.Fail($"Registry key '{subKeyPath}' is protected.");
   }
   ```
3. `SafeRegistryGuard` guarantees:
   - Root Hives cannot be deleted (`HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`).
   - Subkey path depth MUST satisfy $\ge 3$ (e.g., `Software\Microsoft\Windows\CurrentVersion\Run`).
   - Absolute protection for core OS hives (BCD00000000, SAM, SECURITY, SYSTEM, HARDWARE).

---

## ⚡ 4. Zero Command Injection Standards (`ProcessRunner`)

- **FORBIDDEN:** String concatenation of shell commands:
  ```csharp
  // ❌ DANGEROUS: Susceptible to command injection if argument contains shell delimiters: & | ; ` $
  Process.Start("cmd.exe", $"/c ping {host}");
  ```
- **MANDATORY:** Execute binaries directly via `ProcessRunner` using `ArgumentList`:
  ```csharp
  // ✅ SAFE: Arguments isolated cleanly without shell expansion
  var args = new[] { "-n", "4", host };
  var result = await ProcessRunner.RunAsync("ping.exe", args, ct);
  if (!result.Success)
  {
      AppLogger.LogError($"Command failed: {result.StandardError}");
  }
  ```
- Sanitize all external inputs prior to execution via [InputSanitizer.cs](../../Core/Helpers/InputSanitizer.cs).

---

## 🔒 5. Windows Core Service Armor (`ServiceSafetyService`)

- Prior to stopping (`Stop`) or disabling (`Disable`) any service:
  ```csharp
  if (ServiceSafetyService.IsProtectedService(serviceName))
  {
      return OperationResult.Fail($"Cannot modify protected Windows core service: {serviceName}");
  }
  ```
- 45+ critical Windows infrastructure services are perpetually protected: `RpcSs`, `DcomLaunch`, `WinDefend`, `CryptSvc`, `EventLog`, `PlugPlay`, `SamSs`, `LSM`, `wuauserv` (safe mode), etc.
