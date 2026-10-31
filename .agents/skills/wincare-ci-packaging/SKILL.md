---
name: wincare-ci-packaging
description: >-
  Build, publish, and packaging guidelines for WinCare Pro. Covers compiling with dotnet build, running publish.bat for Self-Contained x64 distribution, building the Inno Setup installer via publish_installer.bat, versioning via AppConstants, and pre-release smoke test verification. Use whenever compiling, publishing, releasing, or verifying build outputs.
---

# 📦 WinCare Pro — Build, CI & Packaging Standards (CI & Packaging)

WinCare Pro distributes as a Self-Contained, trimmed, single-folder x64 .NET 10 desktop application packaged with an Inno Setup installer.

## 📁 Skill Assets & Helpers
- **Quiet Verification Script:** [`scripts/verify-build-quiet.ps1`](./scripts/verify-build-quiet.ps1)
- **Reference Examples:**
  - SSOT Versioning: [`examples/AppConstantsVersionExample.cs`](./examples/AppConstantsVersionExample.cs)
- **Checklists & Release Guides:**
  - Release Smoke Test Checklist: [`resources/release-smoke-test-checklist.md`](./resources/release-smoke-test-checklist.md)

---

## ⚡ 1. Build Verification

Always verify compilation using quiet mode:

```powershell
dotnet build WinCarePro.csproj -c Debug -v q --nologo
```

---

## 🚀 2. Production Deployment & Installer

1. **Self-Contained Deployment:**
   ```powershell
   .\publish.bat
   ```
2. **Inno Setup Installer Creation:**
   ```powershell
   .\publish_installer.bat
   ```
