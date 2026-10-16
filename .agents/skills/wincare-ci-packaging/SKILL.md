---
name: wincare-ci-packaging
description: >-
  Build, publish, and packaging guidelines for WinCare Pro. Covers compiling with dotnet build, running publish.bat for Self-Contained x64 distribution, building the Inno Setup installer via publish_installer.bat, versioning via AppConstants, and pre-release smoke test verification. Use whenever compiling, publishing, releasing, or verifying build outputs.
---

# 📦 WinCare Pro — CI Packaging & Release Engineering

Standardized compilation, distribution packaging, and installer generation pipeline for **WinCare Pro Suite v5.0.0 (Polaris)** on Windows 10/11 x64.

---

## ⚡ 1. Command Reference

| Operational Target | CLI Invocation | Engineering Rationale |
| :--- | :--- | :--- |
| **Syntax Verification** | `dotnet build WinCarePro.csproj -c Debug -v q --nologo` | Fast, suppresses extraneous compiler headers (Rule 11) |
| **Full Regression Suite** | `dotnet test WinCarePro.Tests/WinCarePro.Tests.csproj --logger "console;verbosity=minimal"` | Strict gate: 100% Passed required (465 tests) |
| **Self-Contained Deployment** | `.\publish.bat` | Generates standalone x64 binary payload without runtime prerequisites |
| **Inno Setup Installer** | `.\publish_installer.bat` | Compiles Inno Setup script into signed `Setup.exe` |

---

## 🏷️ 2. Single Source of Truth (SSOT) Versioning

Application identity and version metadata **MUST** be defined strictly in [Core/AppConstants.cs](../../Core/AppConstants.cs):

```csharp
public static class AppConstants
{
    public const string AppName = "WinCare Pro";
    public const string Version = "5.0.0";
    public const string BuildNumber = "2026.10";
    public const string Codename = "Polaris";
    public const string Copyright = "© 2026 WinCare Corporation";
}
```

Hardcoding isolated version literals inside XAML views or auxiliary files is STRICTLY FORBIDDEN.

---

## 🚀 3. Self-Contained x64 Packaging Pipeline (`publish.bat`)

When executing `publish.bat`, the script executes:
1. Purges prior `bin\` and `obj\` build caches.
2. Publishes .NET 10.0 x64 Self-Contained payload:
   ```powershell
   dotnet publish WinCarePro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o ./publish/
   ```
3. Bundles essential runtime resources:
   - `Assets/` directory (icons, branding images).
   - `Assets/Aura/` directory (composition shaders, backdrop assets).
   - Native Windows App SDK runtime components and native `sqlite3.dll`.

---

## 🛠️ 4. Inno Setup Packaging (`publish_installer.bat`)

The production installer is compiled via Inno Setup (`ISCC.exe`):
1. Requires administrator privileges (`PrivilegesRequired=admin`) for service and task registration.
2. Validates target Windows environment (minimum Windows 10 Build 19041 x64).
3. Registers deterministic uninstallation keys under Windows Registry (`Uninstall` hive).
4. Generates Desktop and Start Menu application shortcuts.

---

## ✅ 5. Pre-Release Quality Verification Gate

Prior to distributing builds:
- [ ] **1. Clean Build:** `dotnet build -c Release` yields 0 errors and 0 new warnings.
- [ ] **2. 100% Test Pass Rate:** All 465 xUnit test cases pass (`0 Failed`).
- [ ] **3. i18n Dictionary Parity:** Both `vi-VN` and `en-US` dictionaries contain matching keys.
- [ ] **4. Native Binaries:** Confirm `Microsoft.ui.xaml.dll` and `sqlite3.dll` are present in `./publish/`.
- [ ] **5. Cold Start & Working Set:** Cold start $\le 350\text{ ms}$, foreground RAM $\le 85\text{ MB}$.
