<#
.SYNOPSIS
    Runs targeted WinCare Pro unit tests with minimal token output.
.PARAMETER Filter
    Test filter expression (e.g., 'FullyQualifiedName~ProcessRunnerTests' or 'SecurityAndSafetyTests').
#>
param(
    [Parameter(Mandatory = $false)]
    [string]$Filter = ""
)

$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
$testProject = Join-Path $root "WinCarePro.Tests\WinCarePro.Tests.csproj"

$argsList = @("test", $testProject, "--logger", "console;verbosity=minimal")
if (![string]::IsNullOrWhiteSpace($Filter)) {
    $argsList += @("--filter", $Filter)
}

Write-Host "Executing targeted test suite..." -ForegroundColor Cyan
& dotnet @argsList
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "Tests execution completed with EXIT CODE 0 (PASSED)." -ForegroundColor Green
} else {
    Write-Error "Tests failed with EXIT CODE $exitCode."
}
exit $exitCode

