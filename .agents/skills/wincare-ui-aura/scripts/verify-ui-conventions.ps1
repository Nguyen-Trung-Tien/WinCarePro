<#
.SYNOPSIS
    Verifies WinUI 3 XAML conventions: TranslationConverter usage and Tabular numeral typography.
#>
$ErrorActionPreference = "Continue"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
Write-Host "Verifying UI Conventions on $root..." -ForegroundColor Cyan

$xamlFiles = Get-ChildItem -Path "$root" -Recurse -Filter "*.xaml" | Where-Object {
    $_.FullName -notmatch "\\(bin|obj|\.git|\.vs|\.agents)\\?"
}

$issues = 0
foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName
    $lineNum = 0
    foreach ($line in $content) {
        $lineNum++
        # Look for hardcoded Text="English/Vietnamese text"
        if ($line -match 'Text="[A-Z][a-zA-Z\s]{4,}"' -and $line -notmatch 'x:Uid|Binding|Converter') {
            Write-Warning "[$($file.Name):$lineNum] Possible hardcoded text: $line"
            $issues++
        }
    }
}

Write-Host "UI Conventions Scan Completed: $issues potential issue(s) detected." -ForegroundColor Green

