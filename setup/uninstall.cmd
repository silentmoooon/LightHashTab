@echo off
setlocal
cd /d "%~dp0"

set "INSTALL_DIR=%LOCALAPPDATA%\Programs\LightHashTab"
set "DLL_DST=%INSTALL_DIR%\LightHashTab.dll"

echo =====================================================
echo   LightHashTab - Uninstall
echo =====================================================
echo.

echo Unregistering...
regsvr32.exe /u /s "%DLL_DST%" 2>nul

echo Removing files...
if exist "%DLL_DST%" del /f /q "%DLL_DST%"
if exist "%INSTALL_DIR%" rmdir "%INSTALL_DIR%" 2>nul

echo.
echo LightHashTab has been removed.
echo You may need to restart Windows Explorer for the change to take effect.
echo.
pause
