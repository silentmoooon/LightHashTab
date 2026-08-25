@echo off
setlocal
cd /d "%~dp0"

:: =====================================================================
:: LightHashTab - End-User Installer
:: No .NET runtime required. Just run this script.
:: =====================================================================

set "INSTALL_DIR=%LOCALAPPDATA%\Programs\LightHashTab"
set "DLL_SRC=%~dp0LightHashTab.dll"
set "DLL_DST=%INSTALL_DIR%\LightHashTab.dll"

echo =====================================================
echo   LightHashTab - Install
echo =====================================================
echo.

if not exist "%DLL_SRC%" (
    echo ERROR: LightHashTab.dll not found next to this script.
    pause & exit /b 1
)

echo Installing to: %INSTALL_DIR%
echo.

:: Create install directory
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Unregister old version first (ignore errors)
regsvr32.exe /u /s "%DLL_DST%" 2>nul

:: Copy DLL
copy /y "%DLL_SRC%" "%DLL_DST%" >nul
if errorlevel 1 (
    echo ERROR: Could not copy DLL to install directory.
    pause & exit /b 1
)

:: Register
regsvr32.exe /s "%DLL_DST%"
if errorlevel 1 (
    echo ERROR: Registration failed.
    pause & exit /b 1
)

echo SUCCESS!
echo.
echo Right-click any file, open Properties,
echo and look for the "File Hashes" tab.
echo.
pause
