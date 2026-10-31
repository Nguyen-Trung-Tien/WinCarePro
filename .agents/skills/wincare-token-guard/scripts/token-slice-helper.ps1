<#
.SYNOPSIS
    Token-efficient symbol locator and narrow slicer.
.PARAMETER Query
    Symbol or search term.
.PARAMETER FileExtension
    Target file extension (default: '*.cs').
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,

    [string]$FileExtension = "*.cs"
)

$root = Resolve-Path "$PSScriptRoot\..\..\..\.."
Write-Host "Searching for '$Query' in $FileExtension files..." -ForegroundColor Cyan

$results = Get-ChildItem -Path $root -Recurse -Filter $FileExtension | Where-Object {
    $_.FullName -notmatch "\\(bin|obj|\.git|\.vs|\.agents)\\?"
} | Select-String -Pattern $Query

foreach ($m in $results | Select-Object -First 10) {
    Write-Host "$($m.Filename):$($m.LineNumber): $($m.Line.Trim())" -ForegroundColor Green
}

