@echo off
title WinCarePro Packager
echo ===================================================
echo   Publishing WinCarePro as a Self-Contained App
echo ===================================================
echo.
echo [1/2] Cleaning previous builds...
dotnet clean -c Release

echo.
echo [2/3] Restoring packages for win-x64...
dotnet restore -r win-x64

echo.
echo [3/3] Publishing project to single executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o .\PublishOutput

echo.
echo ===================================================
echo   Success! 
echo   Your packaged application is in the 'PublishOutput' folder.
echo   You only need to share 'WinCarePro.exe' from that folder.
echo ===================================================
echo.
pause
