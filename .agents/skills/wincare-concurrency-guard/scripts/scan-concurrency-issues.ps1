<#
.SYNOPSIS
    Scans codebase for concurrency anti-patterns: sync-over-async (.Result, .Wait()) and async void methods.
#>
$ErrorActionPreference = "Continue"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
Write-Host "Scanning for Concurrency Anti-Patterns in $root..." -ForegroundColor Cyan

$issues = 0
$csFiles = Get-ChildItem -Path "$root" -Recurse -Filter "*.cs" | Where-Object {
    $_.FullName -notmatch "\\(bin|obj|\.git|\.vs|WinCarePro\.Tests|\.agents)\\?"
}

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName
    $lineNum = 0
    foreach ($line in $content) {
        $lineNum++
        # Check for .Result
        if ($line -match '\.Result\b' -and $line -notmatch 'OperationResult|//') {
            Write-Warning "[$($file.Name):$lineNum] Possible sync-over-async .Result: $line"
            $issues++
        }
        # Check for .Wait()
        if ($line -match '\.Wait\(\)' -and $line -notmatch 'Task\.Delay|//') {
            Write-Warning "[$($file.Name):$lineNum] Possible sync-over-async .Wait(): $line"
            $issues++
        }
        # Check for async void (excluding event handlers)
        if ($line -match '\basync\s+void\b' -and $line -notmatch 'EventArgs|RoutedEventArgs|//') {
            Write-Warning "[$($file.Name):$lineNum] async void method detected (use async Task): $line"
            $issues++
        }
    }
}

if ($issues -eq 0) {
    Write-Host "Concurrency Scan Passed: 0 anti-patterns detected." -ForegroundColor Green
} else {
    Write-Host "Concurrency Scan Complete: $issues warning(s) detected. Ensure RunOnUI and async Task are used." -ForegroundColor Yellow
}

