<#
.SYNOPSIS
    Performs quiet syntax and compilation verification on WinCare Pro.
#>
$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
$proj = Join-Path $root "WinCarePro.csproj"

Write-Host "Running quiet build verification on $proj..." -ForegroundColor Cyan
& dotnet build $proj -c Debug -v q --nologo
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "Build verification PASSED: 0 errors." -ForegroundColor Green
} else {
    Write-Error "Build verification FAILED with exit code $exitCode."
}
exit $exitCode

