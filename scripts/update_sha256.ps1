# ==============================================================================
# WinCare Pro - Installer SHA-256 Checksum Generator & Manifest Synchronizer
# ==============================================================================
[CmdletBinding()]
param(
    [string]$InstallerPath = ".\PublishOutput\WinCareProSetup.exe",
    [string]$ManifestPath = ".\update.json"
)

$ErrorActionPreference = "Stop"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  WinCare Pro - SHA-256 Synchronizer              " -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan

if (-not (Test-Path $InstallerPath)) {
    Write-Error "Installer file not found at: $InstallerPath"
    exit 1
}

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Update manifest file not found at: $ManifestPath"
    exit 1
}

Write-Host "[1/3] Computing SHA-256 for '$InstallerPath'..." -ForegroundColor Yellow
$hashResult = Get-FileHash -Path $InstallerPath -Algorithm SHA256
$computedHash = $hashResult.Hash.ToLowerInvariant()
Write-Host "  Computed SHA-256: $computedHash" -ForegroundColor Green

Write-Host "[2/3] Updating '$ManifestPath'..." -ForegroundColor Yellow
$manifestContent = Get-Content -Path $ManifestPath -Raw -Encoding utf8
$updatedContent = [System.Text.RegularExpressions.Regex]::Replace(
    $manifestContent,
    '("sha256"\s*:\s*")[a-fA-F0-9]{64}(")',
    "${1}$computedHash${2}"
)
$updatedContent = [System.Text.RegularExpressions.Regex]::Replace(
    $updatedContent,
    '("beta_sha256"\s*:\s*")[a-fA-F0-9]{64}(")',
    "${1}$computedHash${2}"
)

[System.IO.File]::WriteAllText($ManifestPath, $updatedContent, [System.Text.Encoding]::UTF8)
Write-Host "  Successfully synced SHA-256 into $ManifestPath" -ForegroundColor Green

Write-Host "[3/3] Exporting standalone checksum file..." -ForegroundColor Yellow
$checksumPath = "$InstallerPath.sha256"
"$computedHash *$([System.IO.Path]::GetFileName($InstallerPath))" | Set-Content -Path $checksumPath -Encoding utf8
Write-Host "  Checksum file written to: $checksumPath" -ForegroundColor Green

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Sync Completed Successfully!                     " -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Cyan
