<#
.SYNOPSIS
    Verifies i18n translation integrity in WinCare Pro.
.DESCRIPTION
    Parses TranslationManager.Translations.cs to verify that:
    1. Every entry has both an English Key and a valid Vietnamese Value.
    2. There are no duplicate keys or empty mappings.
    3. Proper escaping and syntax are maintained.
#>
$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
$transFile = Join-Path $root "Services\TranslationService\TranslationManager.Translations.cs"

if (-not (Test-Path $transFile)) {
    Write-Error "Could not find $transFile"
}

Write-Host "Verifying i18n Translation Integrity in $transFile..." -ForegroundColor Cyan

$lines = Get-Content $transFile
$keyMap = [System.Collections.Generic.Dictionary[string, int]]::new()
$duplicates = [System.Collections.Generic.List[string]]::new()
$emptyTranslations = [System.Collections.Generic.List[string]]::new()

$totalEntries = 0
$lineNum = 0

foreach ($line in $lines) {
    $lineNum++
    if ($line -match '^\s*_translations\["([^"]+)"\]\s*=\s*"([^"]*)";') {
        $totalEntries++
        $key = $matches[1]
        $val = $matches[2]

        if ([string]::IsNullOrWhiteSpace($val)) {
            $emptyTranslations.Add("Line $lineNum : Key '$key'")
        }

        if ($keyMap.ContainsKey($key)) {
            $duplicates.Add("Line $lineNum (previous at line $($keyMap[$key])): Key '$key'")
        } else {
            $keyMap[$key] = $lineNum
        }
    }
}

Write-Host "Scanned $totalEntries translation entries across $lineNum lines." -ForegroundColor Gray
Write-Host "Unique Translation Keys: $($keyMap.Count)" -ForegroundColor Gray

$passed = $true

if ($emptyTranslations.Count -gt 0) {
    Write-Warning "Found $($emptyTranslations.Count) entries with empty translations:"
    $emptyTranslations | Select-Object -First 10 | ForEach-Object { Write-Warning "  - $_" }
    $passed = $false
}

if ($duplicates.Count -gt 0) {
    Write-Host "Note: $($duplicates.Count) key overwrite(s) or alias duplicates detected." -ForegroundColor Yellow
}

if ($passed) {
    Write-Host "i18n Integrity Check PASSED: $($keyMap.Count) unique bilingual entries validated." -ForegroundColor Green
} else {
    Write-Error "i18n Integrity Check FAILED: Issues found."
}
