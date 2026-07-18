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

echo [3/3] Publishing project to single executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=false -o .\PublishOutput
echo Copying Assets folder...
xcopy /E /I /Y .\Assets .\PublishOutput\Assets

echo.
echo ===================================================
echo   Success! 
echo   Your packaged application is in the 'PublishOutput' folder.
echo   Note: You need to share BOTH 'WinCarePro.exe' and the 'Assets' folder next to it.
echo ===================================================
echo.
timeout /t 3
exit
