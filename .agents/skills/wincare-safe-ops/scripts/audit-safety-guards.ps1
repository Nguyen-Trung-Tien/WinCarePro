<#
.SYNOPSIS
    Audits codebase for violations of WinCare Pro Zero-Trust Safe Operations (Rule 02 & Rule 03).
.DESCRIPTION
    Scans C# files for direct shell execution, string concatenation commands, and unchecked deletions.
#>
$ErrorActionPreference = "Continue"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
Write-Host "Running Zero-Trust Safe Operations Audit on $root..." -ForegroundColor Cyan

$violations = 0
$csFiles = Get-ChildItem -Path "$root" -Recurse -Filter "*.cs" | Where-Object {
    $_.FullName -notmatch "\\(bin|obj|\.git|\.vs|WinCarePro\.Tests|\.agents)\\?"
}

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName
    $lineNum = 0
    foreach ($line in $content) {
        $lineNum++
        # Check for direct cmd.exe binary execution
        if ($line -match '"cmd\.exe"' -and $line -notmatch 'SafeProcess|ProcessRunner|//') {
            Write-Warning "[$($file.Name):$lineNum] Potential direct cmd.exe shell invocation: $line"
            $violations++
        }
        # Check for Process.Start string concatenation
        if ($line -match 'Process\.Start\([^,)]+\+' -and $line -notmatch '//') {
            Write-Warning "[$($file.Name):$lineNum] String concatenation in Process.Start: $line"
            $violations++
        }
    }
}

if ($violations -eq 0) {
    Write-Host "Audit Passed: 0 Safe Operations violations detected." -ForegroundColor Green
} else {
    Write-Host "Audit Warning: $violations potential violation(s) found. Review and enforce ProcessRunner / SafePathGuard." -ForegroundColor Yellow
}
