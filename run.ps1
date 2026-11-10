$ErrorActionPreference = "Stop"
Push-Location "$PSScriptRoot\src"
try {
    if ($args -and $args.Count -gt 0) {
        dotnet run -- @args
    } else {
        dotnet run
    }
}
finally {
    Pop-Location
}



