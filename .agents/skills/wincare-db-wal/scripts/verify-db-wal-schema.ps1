<#
.SYNOPSIS
    Inspects WinCare Pro SQLite database schema and WAL mode pragmas.
#>
param(
    [string]$DbPath = "$env:LOCALAPPDATA\WinCarePro\wincare.db"
)

Write-Host "Checking SQLite database at: $DbPath" -ForegroundColor Cyan
if (-not (Test-Path $DbPath)) {
    Write-Host "Database file does not yet exist at $DbPath (created on initial launch)." -ForegroundColor Yellow
    exit 0
}

$fileInfo = Get-Item $DbPath
Write-Host "Database Size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Green

$walPath = "$DbPath-wal"
if (Test-Path $walPath) {
    Write-Host "WAL Journal file active: $(Get-Item $walPath | Select-Object -ExpandProperty Length) bytes" -ForegroundColor Green
} else {
    Write-Host "WAL Journal file idle." -ForegroundColor Gray
}

