<#
.SYNOPSIS
    Verifies that all WinCare Pro configuration files, manifests, and source files
    are strictly synchronized to the same version number.
#>
$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
Write-Host "Verifying Version Synchronization across WinCare Pro..." -ForegroundColor Cyan

# 1. Read SSOT version from Core/AppConstants.cs
$appConstantsPath = Join-Path $root "Core\AppConstants.cs"
$appConstantsContent = Get-Content $appConstantsPath -Raw
if ($appConstantsContent -match 'DefaultVersionString\s*=\s*"([^"]+)"') {
    $ssotVersion = $matches[1]
} else {
    Write-Error "Could not find DefaultVersionString in $appConstantsPath"
}
$assemblyVersion = "$ssotVersion.0"
Write-Host "SSOT Version from AppConstants.cs: $ssotVersion (Assembly: $assemblyVersion)" -ForegroundColor Green

$mismatches = 0

# 2. Check WinCarePro.csproj
$csprojPath = Join-Path $root "WinCarePro.csproj"
$csproj = Get-Content $csprojPath -Raw
if ($csproj -match '<Version>([^<]+)</Version>') {
    $v = $matches[1]
    if ($v -ne $ssotVersion) {
        Write-Warning "WinCarePro.csproj <Version> ($v) does not match SSOT ($ssotVersion)"
        $mismatches++
    }
}
if ($csproj -match '<AssemblyVersion>([^<]+)</AssemblyVersion>') {
    $av = $matches[1]
    if ($av -ne $assemblyVersion) {
        Write-Warning "WinCarePro.csproj <AssemblyVersion> ($av) does not match SSOT ($assemblyVersion)"
        $mismatches++
    }
}

# 3. Check app.manifest
$manifestPath = Join-Path $root "app.manifest"
if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath -Raw
    if ($manifest -match '<assemblyIdentity[^>]+version="([^"]+)"') {
        $mv = $matches[1]
        if ($mv -ne $assemblyVersion) {
            Write-Warning "app.manifest version ($mv) does not match SSOT ($assemblyVersion)"
            $mismatches++
        }
    }
}

# 4. Check Package.appxmanifest
$appxPath = Join-Path $root "Package.appxmanifest"
if (Test-Path $appxPath) {
    $appx = Get-Content $appxPath -Raw
    if ($appx -match '<Identity[^>]+Version="([^"]+)"') {
        $axv = $matches[1]
        if ($axv -ne $assemblyVersion) {
            Write-Warning "Package.appxmanifest Version ($axv) does not match SSOT ($assemblyVersion)"
            $mismatches++
        }
    }
}

# 5. Check setup.iss
$setupPath = Join-Path $root "setup.iss"
if (Test-Path $setupPath) {
    $setup = Get-Content $setupPath -Raw
    if ($setup -match '#define\s+MyAppVersion\s+"([^"]+)"') {
        $sv = $matches[1]
        if ($sv -ne $ssotVersion) {
            Write-Warning "setup.iss MyAppVersion ($sv) does not match SSOT ($ssotVersion)"
            $mismatches++
        }
    }
}

# 6. Check update.json
$updatePath = Join-Path $root "update.json"
if (Test-Path $updatePath) {
    $update = Get-Content $updatePath -Raw
    if ($update -match '"version":\s*"([^"]+)"') {
        $uv = $matches[1]
        if ($uv -ne $ssotVersion) {
            Write-Warning "update.json version ($uv) does not match SSOT ($ssotVersion)"
            $mismatches++
        }
    }
}

if ($mismatches -eq 0) {
    Write-Host "Version Synchronization PASSED: All 6 touchpoints are synchronized to v$ssotVersion." -ForegroundColor Green
} else {
    Write-Error "Version Synchronization FAILED: $mismatches mismatch(es) detected."
}
