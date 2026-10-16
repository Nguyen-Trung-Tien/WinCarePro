# 🧬 09. Windows Registry & Win32 Interop Rules

> [⬅️ 08. Release Packaging & CI/CD](08_RELEASE_PACKAGING_AND_CICD_RULES.md) • [🏠 Rules Index](README.md) • **Rule 09** • [Next Rule: 10. Incident Management & Logging ➡️](10_INCIDENT_MANAGEMENT_AND_LOGGING_RULES.md)

---

## 🗂️ 1. Windows Registry Mutation Governance

WinCare Pro interacts with the Windows Registry for performance tuning, context menu configuration, startup management, and residual cleanup.

### 🔴 MANDATORY INVARIANTS:
1. **Validation via `SafeRegistryGuard`:**
   - All registry key deletions and value mutations **MUST** pass [SafeRegistryGuard.cs](../../Core/Helpers/SafeRegistryGuard.cs) validation.
   - Deletion of Root Hives (`HKLM`, `HKCU`, `HKCR`, `HKU`, `HKCC`) is STRICTLY FORBIDDEN.
   - Deletion of vital system prefixes (`HKLM\SYSTEM\CurrentControlSet\Control`, `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion`) is STRICTLY FORBIDDEN.
   - Subkey path depth MUST be at least 3 segments (`SegmentCount >= 3`).
   - Modification or deletion of essential shell boot keys (`Shell`, `Userinit`) is STRICTLY FORBIDDEN.
2. **Mandatory Pre-Mutation Backup:**
   - Before modifying or deleting any Registry key/value, **ALWAYS** invoke `RegistryBackupEngine.BackupKey()` to export an atomic `.reg` rollback artifact.
3. **Dual 32-bit & 64-bit Registry Views (`RegistryView`):**
   - When querying installed applications or startup entries, ALWAYS query both `RegistryView.Registry64` and `RegistryView.Registry32` (`WOW6432Node`) to avoid missing entries.

```csharp
// Canonical cross-architecture registry access pattern:
using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
using var subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", writable: false);
```

---

## 🔌 2. P/Invoke & Native Win32 Interop Standards

1. **Source-Generated `[LibraryImport]` Declarations:**
   - Win32 interop MUST use `[LibraryImport]` source generators (`SetLastError = true`, `StringMarshalling = StringMarshalling.Utf16`). Legacy `[DllImport]` is strictly deprecated.
   - Check status codes and retrieve error details via `Marshal.GetLastPInvokeError()` or `Marshal.GetLastPInvokeErrorMessage()` when native calls fail (`FALSE` or `INVALID_HANDLE_VALUE`).
2. **Handle Lifecycle Management (`SafeHandle` & `CloseHandle`):**
   - Native handles (`IntPtr`) returned from `OpenProcess`, `OpenProcessToken`, `CreateFile` **MUST** be closed deterministically or encapsulated in `SafeHandle` derivations to eliminate kernel handle leaks.
3. **Explicit Struct Sizing:**
   - Win32 structures (e.g. `MEMORYSTATUSEX`, `WINTRUST_DATA`, `SHELLEXECUTEINFO`) MUST initialize size fields `dwLength = (uint)Marshal.SizeOf<STRUCT>()` before native dispatch.

---

## 📡 3. WMI / CIM Query Standards (`System.Management`)

1. **Deterministic Resource Release:**
   - All `ManagementObjectSearcher` and `ManagementObjectCollection` instances MUST be wrapped in `using` declarations to terminate underlying COM WMI proxy channels.
2. **Timeout Bounds & Graceful Degradation:**
   - Windows WMI services (`Winmgmt`) can stall or corrupt. WMI queries MUST execute within strict timeout bounds via `Task.Run` with a `CancellationToken`. When WMI faults occur, fallback immediately to direct Registry or Performance Counter equivalents.

---

<div align="center">
  <sub>[🏠 Rules Index](README.md) • WinCare Pro Suite Production Engineering Governance</sub>
</div>
