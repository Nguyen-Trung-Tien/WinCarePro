<#
.SYNOPSIS
    Atomically bumps the application version across all WinCare Pro touchpoints.
.DESCRIPTION
    Updates AppConstants.cs, WinCarePro.csproj, app.manifest, Package.appxmanifest,
    setup.iss, update.json, and AppVersionManagementTests.cs in one unified operation.
.PARAMETER NewVersion
    The target semantic version string (e.g., '5.1.2').
.PARAMETER Codename
    The release codename (default: 'Polaris').
.PARAMETER BuildDate
    The ISO build date (default: today's date YYYY-MM-DD).
.PARAMETER DryRun
    If specified, displays planned changes without writing to disk.
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$NewVersion,

    [string]$Codename = "Polaris",

    [string]$BuildDate = (Get-Date -Format "yyyy-MM-dd"),

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
$assemblyVersion = "$NewVersion.0"
$vParts = $NewVersion.Split('.')
$major = [int]$vParts[0]
$minor = [int]$vParts[1]
$patch = [int]$vParts[2]

Write-Host "Initiating WinCare Pro Version Bump to v$NewVersion (Assembly: $assemblyVersion, Codename: $Codename, BuildDate: $BuildDate)..." -ForegroundColor Cyan

# 1. Update Core/AppConstants.cs
$appConstantsPath = if (Test-Path (Join-Path $root "src\Core\AppConstants.cs")) { Join-Path $root "src\Core\AppConstants.cs" } else { Join-Path $root "Core\AppConstants.cs" }
$c1 = Get-Content $appConstantsPath -Raw
$c1 = [regex]::Replace($c1, 'public const string DefaultVersionString = "[^"]+";', "public const string DefaultVersionString = `"$NewVersion`";")
$c1 = [regex]::Replace($c1, 'public const string DefaultAssemblyVersionString = "[^"]+";', "public const string DefaultAssemblyVersionString = `"$assemblyVersion`";")
$c1 = [regex]::Replace($c1, 'public const string DefaultBuildDate = "[^"]+";', "public const string DefaultBuildDate = `"$BuildDate`";")
$c1 = [regex]::Replace($c1, 'new Version\(\d+, \d+, \d+, \d+\)', "new Version($major, $minor, $patch, 0)")
if (-not $DryRun) { Set-Content -Path $appConstantsPath -Value $c1 -NoNewline }
Write-Host " [OK] Core/AppConstants.cs" -ForegroundColor Green

# 2. Update WinCarePro.csproj and Directory.Build.props
$csprojPath = if (Test-Path (Join-Path $root "src\WinCarePro.csproj")) { Join-Path $root "src\WinCarePro.csproj" } else { Join-Path $root "WinCarePro.csproj" }
$c2 = Get-Content $csprojPath -Raw
$c2 = [regex]::Replace($c2, '<Version>[^<]+</Version>', "<Version>$NewVersion</Version>")
$c2 = [regex]::Replace($c2, '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$assemblyVersion</AssemblyVersion>")
$c2 = [regex]::Replace($c2, '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$assemblyVersion</FileVersion>")
$c2 = [regex]::Replace($c2, '<InformationalVersion>[^<]+</InformationalVersion>', "<InformationalVersion>$NewVersion (Codename: $Codename)</InformationalVersion>")
$c2 = [regex]::Replace($c2, '<Product>[^<]+</Product>', "<Product>WinCare Pro v$NewVersion</Product>")
if (-not $DryRun) { Set-Content -Path $csprojPath -Value $c2 -NoNewline }
Write-Host " [OK] WinCarePro.csproj" -ForegroundColor Green

$propsPath = Join-Path $root "Directory.Build.props"
if (Test-Path $propsPath) {
    $cp = Get-Content $propsPath -Raw
    $cp = [regex]::Replace($cp, '<Version>[^<]+</Version>', "<Version>$NewVersion</Version>")
    $cp = [regex]::Replace($cp, '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$assemblyVersion</AssemblyVersion>")
    $cp = [regex]::Replace($cp, '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$assemblyVersion</FileVersion>")
    $cp = [regex]::Replace($cp, '<InformationalVersion>[^<]+</InformationalVersion>', "<InformationalVersion>$NewVersion (Codename: $Codename)</InformationalVersion>")
    if (-not $DryRun) { Set-Content -Path $propsPath -Value $cp -NoNewline }
    Write-Host " [OK] Directory.Build.props" -ForegroundColor Green
}

# 3. Update app.manifest
$manifestPath = if (Test-Path (Join-Path $root "src\app.manifest")) { Join-Path $root "src\app.manifest" } else { Join-Path $root "app.manifest" }
if (Test-Path $manifestPath) {
    $c3 = Get-Content $manifestPath -Raw
    $c3 = [regex]::Replace($c3, '<assemblyIdentity\s+version="[^"]+"', "<assemblyIdentity version=`"$assemblyVersion`"")
    if (-not $DryRun) { Set-Content -Path $manifestPath -Value $c3 -NoNewline }
    Write-Host " [OK] app.manifest" -ForegroundColor Green
}

# 4. Update Package.appxmanifest
$appxPath = if (Test-Path (Join-Path $root "src\Package.appxmanifest")) { Join-Path $root "src\Package.appxmanifest" } else { Join-Path $root "Package.appxmanifest" }
if (Test-Path $appxPath) {
    $c4 = Get-Content $appxPath -Raw
    $c4 = [regex]::Replace($c4, '(?i)(<Identity[^>]+Version=")[^"]+(")', "`${1}$assemblyVersion`${2}")
    if (-not $DryRun) { Set-Content -Path $appxPath -Value $c4 -NoNewline }
    Write-Host " [OK] Package.appxmanifest" -ForegroundColor Green
}

# 5. Update setup.iss
$setupPath = if (Test-Path (Join-Path $root "build\setup.iss")) { Join-Path $root "build\setup.iss" } else { Join-Path $root "setup.iss" }
if (Test-Path $setupPath) {
    $c5 = Get-Content $setupPath -Raw
    $c5 = [regex]::Replace($c5, '#define\s+MyAppVersion\s+"[^"]+"', "#define MyAppVersion `"$NewVersion`"")
    if (-not $DryRun) { Set-Content -Path $setupPath -Value $c5 -NoNewline }
    Write-Host " [OK] setup.iss" -ForegroundColor Green
}

# 6. Update update.json
$updatePath = if (Test-Path (Join-Path $root "build\update.json")) { Join-Path $root "build\update.json" } else { Join-Path $root "update.json" }
if (Test-Path $updatePath) {
    $c6 = Get-Content $updatePath -Raw
    $c6 = [regex]::Replace($c6, '"version":\s*"[^"]+"', "`"version`": `"$NewVersion`"")
    $c6 = [regex]::Replace($c6, '"beta_version":\s*"[^"]+"', "`"beta_version`": `"$NewVersion-beta`"")
    $c6 = [regex]::Replace($c6, 'releases/download/v[^/]+/WinCareProSetup\.exe', "releases/download/v$NewVersion/WinCareProSetup.exe")
    $c6 = [regex]::Replace($c6, 'releases/download/v[^/]+/WinCareProSetup_Beta\.exe', "releases/download/v$NewVersion-beta/WinCareProSetup_Beta.exe")
    if (-not $DryRun) { Set-Content -Path $updatePath -Value $c6 -NoNewline }
    Write-Host " [OK] update.json" -ForegroundColor Green
}

# 7. Update WinCarePro.Tests
$testPath = if (Test-Path (Join-Path $root "WinCarePro.Tests\Core\AppVersionManagementTests.cs")) { Join-Path $root "WinCarePro.Tests\Core\AppVersionManagementTests.cs" } else { Join-Path $root "WinCarePro.Tests\AppVersionManagementTests.cs" }
if (Test-Path $testPath) {
    $c7 = Get-Content $testPath -Raw
    $c7 = [regex]::Replace($c7, 'Assert\.Equal\(\d+,\s*AppConstants\.CurrentVersion\.Major\);', "Assert.Equal($major, AppConstants.CurrentVersion.Major);")
    $c7 = [regex]::Replace($c7, 'Assert\.Equal\(\d+,\s*AppConstants\.CurrentVersion\.Minor\);', "Assert.Equal($minor, AppConstants.CurrentVersion.Minor);")
    $c7 = [regex]::Replace($c7, 'Assert\.Equal\(\d+,\s*AppConstants\.CurrentVersion\.Build\);', "Assert.Equal($patch, AppConstants.CurrentVersion.Build);")
    $c7 = [regex]::Replace($c7, 'Assert\.Equal\("[^"]+",\s*AppConstants\.VersionString\);', "Assert.Equal(`"$NewVersion`", AppConstants.VersionString);")
    $c7 = [regex]::Replace($c7, 'Assert\.Equal\("v[^"]+",\s*AppConstants\.DisplayVersion\);', "Assert.Equal(`"v$NewVersion`", AppConstants.DisplayVersion);")
    $c7 = [regex]::Replace($c7, 'Assert\.Equal\("v[^"]+",\s*AppConstants\.DisplayVersionFull\);', "Assert.Equal(`"v$NewVersion`", AppConstants.DisplayVersionFull);")
    $c7 = [regex]::Replace($c7, 'Assert\.Contains\("WinCare Pro v[^"]+",\s*AppConstants\.TitleWithVersion\);', "Assert.Contains(`"WinCare Pro v$NewVersion`", AppConstants.TitleWithVersion);")
    $c7 = [regex]::Replace($c7, 'Assert\.Contains\("Version [^"]+ \(Codename:\s*[^)]+\)",\s*AppConstants\.SystemBadgeText\);', "Assert.Contains(`"Version $NewVersion (Codename: $Codename)`", AppConstants.SystemBadgeText);")
    if (-not $DryRun) { Set-Content -Path $testPath -Value $c7 -NoNewline }
    Write-Host " [OK] WinCarePro.Tests/AppVersionManagementTests.cs" -ForegroundColor Green
}

Write-Host "`nValidating synchronization with verify-version-sync.ps1..." -ForegroundColor Cyan
& powershell -ExecutionPolicy Bypass -File "$PSScriptRoot\verify-version-sync.ps1"
Write-Host "Version bump to v$NewVersion completed successfully." -ForegroundColor Green

