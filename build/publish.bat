@echo off
title WinCarePro Packager
setlocal enabledelayedexpansion
pushd "%~dp0.."
echo ===================================================
echo   Publishing WinCarePro as a Self-Contained App
echo ===================================================
echo.
echo [1/2] Cleaning previous builds...
dotnet clean src\WinCarePro.csproj -c Release

echo.
echo [2/3] Restoring packages for win-x64...
dotnet restore src\WinCarePro.csproj -r win-x64

echo [3/3] Publishing project to folder...
dotnet publish src\WinCarePro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -o .\PublishOutput
echo Copying Assets folder...
xcopy /E /I /Y src\Assets .\PublishOutput\Assets

echo.
echo ===================================================
echo   Success! 
echo   Your packaged application is in the 'PublishOutput' folder.
echo   Note: You need to share BOTH 'WinCarePro.exe' and the 'Assets' folder next to it.
echo ===================================================
echo.
popd
timeout /t 3
exit
