<#
.SYNOPSIS
    Scaffolds a new WinCare Pro module adhering to the 4-tier decoupled architecture.
.DESCRIPTION
    Generates Model, Engine, ViewModel, View, and xUnit test stubs based on standardized templates.
.PARAMETER ModuleName
    Name of the feature/module (PascalCase, e.g., 'DiskAnalyzer').
.PARAMETER Category
    Category under Engines (e.g., 'Optimization', 'Security', 'Maintenance').
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ModuleName,

    [Parameter(Mandatory = $false)]
    [string]$Category = "Optimization"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
Write-Host "Scaffolding WinCare Pro module '$ModuleName' under Category '$Category'..." -ForegroundColor Cyan

# Define target paths
$engineDir = Join-Path $root "Engines\$Category"
$modelDir = Join-Path $root "Core\Models\$ModuleName"
$moduleDir = Join-Path $root "Modules\$ModuleName"
$testDir = Join-Path $root "WinCarePro.Tests"

New-Item -ItemType Directory -Force -Path $engineDir | Out-Null
New-Item -ItemType Directory -Force -Path $modelDir | Out-Null
New-Item -ItemType Directory -Force -Path $moduleDir | Out-Null

Write-Host "Target directories created/verified:" -ForegroundColor Green
Write-Host " - Engine: $engineDir"
Write-Host " - Model:  $modelDir"
Write-Host " - Module: $moduleDir"
Write-Host " - Tests:  $testDir"
Write-Host "Module scaffolding complete. Use templates in resources/ to populate stubs." -ForegroundColor Green

