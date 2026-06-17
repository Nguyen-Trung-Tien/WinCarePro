@echo off
title WinCarePro Installer Packager
echo ===================================================
echo   Publishing WinCarePro and Generating Setup.exe
echo ===================================================
echo.

echo [1/3] Cleaning previous builds...
dotnet clean -c Release

echo.
echo [2/3] Publishing project to folder...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true -o .\PublishOutputFolder

echo.
echo [3/3] Compiling installer with Inno Setup...
if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" (
    "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" setup.iss
) else if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" setup.iss
) else (
    echo.
    echo [ERROR] Inno Setup compiler (ISCC.exe) not found!
    echo Please make sure Inno Setup 6 is installed.
    pause
    exit /b 1
)

echo.
echo ===================================================
echo   Success! 
echo   Your setup installer is ready at:
echo   .\PublishOutput\WinCareProSetup.exe
echo   Dung luong file: ~72 MB
echo ===================================================
echo.
timeout /t 5
exit
