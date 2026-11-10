<#
.SYNOPSIS
    WinCare Pro - Safe Artifact Cleaner
    Removes ephemeral build outputs (bin, obj, PublishOutput, TestResults) to maintain a pristine workspace.
#>

[CmdletBinding()]
param()

Write-Host "Cleaning WinCare Pro build artifacts..." -ForegroundColor Cyan

$directoriesToClean = @(
    "bin",
    "obj",
    "PublishOutput",
    "PublishOutputFolder",
    "TestResults",
    "WinCarePro.Tests\bin",
    "WinCarePro.Tests\obj"
)

foreach ($dir in $directoriesToClean) {
    if (Test-Path $dir) {
        Write-Host "Removing: $dir" -ForegroundColor Gray
        Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "✅ Workspace cleaned successfully." -ForegroundColor Green
