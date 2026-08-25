@echo off
setlocal
cd /d "%~dp0"

set "PUBLISH_DIR=%~dp0src\LightHashTab\bin\Release\net10.0-windows\win-x64\publish"
set "DLL=%PUBLISH_DIR%\LightHashTab.dll"

echo =====================================================
echo   lightHashTab - Install / Update
echo =====================================================
echo.

:: Step 1: Unregister old version (ignore errors)
echo [1/4] Unregistering old version...
regsvr32.exe /u /s "%DLL%" 2>nul

:: Step 2: Kill Explorer to release DLL file lock
echo [2/4] Stopping Explorer...
taskkill /f /im explorer.exe >nul 2>&1
ping -n 3 127.0.0.1 >nul

:: Step 3: Build
echo [3/4] Building...
dotnet publish src\LightHashTab\LightHashTab.csproj -c Release -r win-x64
if errorlevel 1 (
    echo.
    echo ERROR: Build failed.
    echo Restarting Explorer...
    start explorer.exe
    pause
    exit /b 1
)

:: Step 4: Register
echo.
echo [4/4] Registering...
regsvr32.exe /s "%DLL%"
if errorlevel 1 (
    echo ERROR: Registration failed.
) else (
    echo SUCCESS: Registered OK.
)

:: Restart Explorer
echo.
echo Restarting Explorer...
start explorer.exe
echo.
echo Done! Right-click a file, open Properties, look for "File Hashes" tab.
echo.
pause
