@echo off
setlocal
cd /d "%~dp0"

echo ========================================================
echo   lightHashTab - Uninstalling Property Sheet Extension
echo ========================================================
echo.

set "DLL_PATH=%~dp0src\LightHashTab\bin\Release\net10.0-windows\win-x64\publish\LightHashTab.dll"

if not exist "%DLL_PATH%" (
    echo [WARNING] Could not find compiled DLL at:
    echo "%DLL_PATH%"
    echo.
)

echo Unregistering COM DLL:
echo "%DLL_PATH%"
echo.

regsvr32.exe /u "%DLL_PATH%"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] lightHashTab uninstalled successfully!
) else (
    echo.
    echo [ERROR] Unregistration failed. Please make sure you ran this script as Administrator.
)

echo.
pause
