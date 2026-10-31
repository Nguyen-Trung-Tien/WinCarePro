<#
.SYNOPSIS
    Scans codebase for silent catch blocks: catch { } or catch (Exception) { } without logging.
#>
$ErrorActionPreference = "Continue"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
Write-Host "Scanning for Silent Catch blocks across $root..." -ForegroundColor Cyan

$issues = 0
$csFiles = Get-ChildItem -Path "$root" -Recurse -Filter "*.cs" | Where-Object {
    $_.FullName -notmatch "\\(bin|obj|\.git|\.vs|WinCarePro\.Tests|\.agents)\\?"
}

foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        # Match catch { or catch (...) { with empty or whitespace body
        if ($line -match 'catch\s*(\([^)]*\))?\s*\{\s*\}') {
            Write-Warning "[$($file.Name):$($i+1)] Empty catch block: $line"
            $issues++
        }
    }
}

if ($issues -eq 0) {
    Write-Host "Scan Passed: Zero empty catch blocks detected." -ForegroundColor Green
} else {
    Write-Host "Scan Finished: $issues empty catch block(s) detected. Enforce CrashLogger.LogError." -ForegroundColor Yellow
}

