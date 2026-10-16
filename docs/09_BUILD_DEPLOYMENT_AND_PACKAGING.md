# 📦 09. Build, Deployment & Packaging (CI/CD)

> [⬅️ 08. Automated Testing & QA](08_TESTING_AND_QUALITY_ASSURANCE.md) • [🏠 Docs Index](README.md) • **Chapter 09** • [Next: 10. Developer Onboarding Guide ➡️](10_DEVELOPER_ONBOARDING_GUIDE.md)

This chapter provides comprehensive instructions for compilation, release publishing, installer packaging via **Inno Setup**, **Auto-Update** configuration, and **GitHub Actions CI/CD** pipelines in **WinCare Pro Suite**.

---

## 🛠️ 1. Build Prerequisites

To compile and package WinCare Pro Suite locally, the development workstation requires:

1. **Operating System:** Windows 10 (Build 19041+) or Windows 11 (x64 recommended).
2. **SDK:** [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or latest).
3. **Windows App SDK:** Version 2.2.0 (configured in `WinCarePro.csproj`).
4. **Installer Packaging:** [Inno Setup 6.x](https://jrsoftware.org/isdl.php) with `ISCC.exe` added to system PATH.
5. **Recommended IDE:** Visual Studio 2022 (v17.10+) with *.NET Desktop Development* and *Windows App SDK C# Templates*.

---

## 🚀 2. Release Publishing Pipeline

WinCare Pro targets a **Self-Contained Single-File (x64)** deployment model, bundling the .NET 10 Runtime directly so end-users can run the application without external dependencies.

### 2.1. Direct Compilation via .NET CLI

```powershell
dotnet publish WinCarePro.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "./PublishOutputFolder"
```

### 2.2. Automated Build Script: [publish.bat](../publish.bat)
Execute `publish.bat` from the repository root to clean legacy `bin/` and `obj/` output folders, build dependencies, and publish the Release binary to `PublishOutputFolder`.

---

## 📦 3. Installer Packaging (Inno Setup)

- **Configuration File:** [setup.iss](../setup.iss)
- **Packaging Execution Script:** [publish_installer.bat](../publish_installer.bat)

### 3.1. WinCare Pro Installer Features:
- **Mandatory Elevation:** `PrivilegesRequired=admin` ensures registration of Registry keys, scheduled tasks, and service configurations.
- **Process Mutex Shutdown:** If WinCare Pro or the Desktop HUD Widget is actively running, the installer signals shutdown before overwriting binary files.
- **Clean Uninstaller Integration:** Cleans temporary caches with an optional prompt to remove `%LocalAppData%\WinCarePro\` databases.
- **Fluent Desktop & Start Menu Shortcuts:** Creates compliant Fluent Design shortcuts and icons.

### 3.2. Packaging via Command Line

```powershell
iscc setup.iss
```
The compiled output is saved to: `PublishOutput/WinCareProSetup.exe`.

---

## 🔄 4. Automated Update Architecture (Auto-Update)

- **Configuration Template:** [update.json](../update.json)
- **JSON Version Verification Schema:**

```json
{
  "version": "5.1.1",
  "url": "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v5.1.1/WinCareProSetup.exe",
  "downloadUrl": "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v5.1.1/WinCareProSetup.exe",
  "sha256": "99fd2e278f7c359901099dd683913458a2408e5abbbaa9eed874516dac41e6e5",
  "changelog": "v5.1.1 (Polaris Release): Comprehensive system maintenance, complete LibraryImport source-generation migration, Zero-Trust registry backups and composition micro-interactions.",
  "beta_version": "5.1.1-beta",
  "beta_url": "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v5.1.1-beta/WinCareProSetup_Beta.exe",
  "beta_downloadUrl": "https://github.com/Nguyen-Trung-Tien/WinCarePro/releases/download/v5.1.1-beta/WinCareProSetup_Beta.exe",
  "beta_sha256": "99fd2e278f7c359901099dd683913458a2408e5abbbaa9eed874516dac41e6e5",
  "beta_changelog": "WinCare Pro v5.1.1 (Polaris Release)"
}
```

### 4.1. Update Verification & Staging Workflow:
1. Application transmits an HTTP GET request to `update.json` on GitHub Releases.
2. Compares semantic versioning strings (`NewVersion > CurrentVersion`).
3. If a newer build exists, prompts the user with localized release changelog notes.
4. Stages `WinCareProSetup.exe` into `%TEMP%\WinCareProUpdate\`, validating binary integrity against the expected SHA-256 hash.
5. Launches the installer with the `/SILENT` parameter and exits to allow seamless binary replacement.

---

## 🔄 5. Continuous Integration Pipeline (GitHub Actions CI/CD)

- **Workflow Configuration:** `.github/workflows/ci.yml`
- **Automated Workflow Stages on Push/PR:**

```mermaid
graph LR
    Checkout["1. Checkout Code"] --> SetupDotnet["2. Setup .NET 10 SDK"]
    SetupDotnet --> Restore["3. Restore NuGet Packages"]
    Restore --> Build["4. Compile Release Solution"]
    Build --> Test["5. Run xUnit 465 Tests"]
    Test --> Pack["6. Package Inno Setup Installer"]
    Pack --> Release["7. Publish Artifacts / GitHub Release"]
```

---

> [⬅️ 08. Automated Testing & QA](08_TESTING_AND_QUALITY_ASSURANCE.md) • [🏠 Docs Index](README.md) • **Chapter 09** • [Next: 10. Developer Onboarding Guide ➡️](10_DEVELOPER_ONBOARDING_GUIDE.md)

