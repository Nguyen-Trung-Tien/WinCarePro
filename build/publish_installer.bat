@echo off
title WinCarePro Installer Packager
setlocal enabledelayedexpansion
pushd "%~dp0.."
echo ===================================================
echo   Publishing WinCarePro and Generating Setup.exe
echo ===================================================
echo.

echo [1/3] Cleaning previous builds...
dotnet clean src\WinCarePro.csproj -c Release
if exist .\PublishOutputFolder rmdir /s /q .\PublishOutputFolder
if exist .\PublishOutput rmdir /s /q .\PublishOutput
mkdir .\PublishOutput
mkdir .\PublishOutputFolder

echo.
echo [2/3] Publishing project to folder...
dotnet publish src\WinCarePro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -o .\PublishOutputFolder
echo Copying Assets folder...
xcopy /E /I /Y src\Assets .\PublishOutputFolder\Assets

echo.
echo [3/3] Compiling installer with Inno Setup...
if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" (
    "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" build\setup.iss
) else if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" build\setup.iss
) else if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    "%ProgramFiles%\Inno Setup 6\ISCC.exe" build\setup.iss
) else (
    echo.
    echo [ERROR] Inno Setup compiler [ISCC.exe] not found!
    echo Please make sure Inno Setup 6 is installed.
    popd
    pause
    exit /b 1
)

echo.
echo [4/4] Calculating SHA-256 and synchronizing update manifest...
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\scripts\update_sha256.ps1 -InstallerPath .\PublishOutput\WinCareProSetup.exe -ManifestPath .\build\update.json
if errorlevel 1 (
    echo [WARNING] Failed to synchronize SHA-256 to update.json!
)

echo.
echo ===================================================
echo   Success! 
echo   Your setup installer is ready at:
echo   .\PublishOutput\WinCareProSetup.exe
echo   Checksum and update.json have been synchronized!
echo ===================================================
echo.
popd
timeout /t 5
exit
