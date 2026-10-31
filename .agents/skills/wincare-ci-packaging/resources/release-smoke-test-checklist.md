# 🚀 WinCare Pro — Release Packaging Smoke Test Checklist

## Pre-Publish Verification Gate
- [ ] 1. Syntax check: Run `.\.agents\skills\wincare-ci-packaging\scripts\verify-build-quiet.ps1` (0 Errors).
- [ ] 2. Regression tests: All xUnit tests pass (`dotnet test`).
- [ ] 3. i18n Sync: `.\.agents\skills\wincare-i18n-sync\scripts\verify-i18n-parity.ps1` passes (100% dictionary synchronization).
- [ ] 4. Versioning: `AppConstants.cs` updated to target version and codename.
- [ ] 5. Self-Contained Build: Run `.\publish.bat` (outputs self-contained x64 deployment to `publish/`).
- [ ] 6. Inno Setup: Run `.\publish_installer.bat` (compiles `WinCarePro_Setup.exe`).
- [ ] 7. Smoke Launch: Install and verify cold start $\le 350\text{ ms}$, idle RAM $\le 15\text{ MB}$.

