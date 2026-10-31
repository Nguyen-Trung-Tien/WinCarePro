# 🔢 WinCare Pro — Version Synchronization Touchpoints Matrix

When bumping or upgrading the application version in WinCare Pro, all the following files **MUST** be updated in exact synchronization:

| No. | File Path | XML Tag / Line / Field | Example Value |
| :---: | :--- | :--- | :--- |
| **01** | `Core/AppConstants.cs` | `DefaultVersionString`<br/>`DefaultAssemblyVersionString`<br/>`DefaultBuildDate` | `"5.1.1"`<br/>`"5.1.1.0"`<br/>`"2026-10-16"` |
| **02** | `WinCarePro.csproj` | `<Version>`<br/>`<AssemblyVersion>`<br/>`<FileVersion>`<br/>`<InformationalVersion>`<br/>`<Product>` | `<Version>5.1.1</Version>`<br/>`<AssemblyVersion>5.1.1.0</AssemblyVersion>`<br/>`<FileVersion>5.1.1.0</FileVersion>`<br/>`5.1.1 (Codename: Polaris)`<br/>`WinCare Pro v5.1.1` |
| **03** | `app.manifest` | `<assemblyIdentity version="..."/>` | `version="5.1.1.0"` |
| **04** | `Package.appxmanifest` | `<Identity Version="..."/>` | `Version="5.1.1.0"` |
| **05** | `setup.iss` | `#define MyAppVersion "..."` | `#define MyAppVersion "5.1.1"` |
| **06** | `update.json` | `"version"`<br/>`"url"`<br/>`"downloadUrl"` | `"5.1.1"`<br/>`https://.../download/v5.1.1/WinCareProSetup.exe` |
| **07** | `WinCarePro.Tests/AppVersionManagementTests.cs` | `Assert.Equal(5, Major)`<br/>`Assert.Equal("5.1.1", VersionString)` | Sync asserts with target version |
| **08** | `Services/TranslationService/TranslationManager.Translations.cs` | `_translations["v5.1.1 (Polaris)"]` | Register bilingual version badges |
| **09** | `CHANGELOG.md` | `## [5.1.1] - YYYY-MM-DD` | New version release notes section |
| **10** | `RELEASE_NOTES.md` | Document header & summary | Vietnamese release overview |
| **11** | `AGENTS.md` & `README.md` | Header badge `v5.1.1 (Polaris)` | Documentation synchronization |

