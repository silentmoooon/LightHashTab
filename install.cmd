@echo off
setlocal
cd /d "%~dp0"

echo ========================================================
echo   lightHashTab - Installing Property Sheet Extension
echo ========================================================
echo.

set "DLL_PATH=%~dp0src\LightHashTab\bin\Release\net10.0-windows\win-x64\publish\LightHashTab.dll"

if not exist "%DLL_PATH%" (
    echo [ERROR] Could not find compiled DLL at:
    echo "%DLL_PATH%"
    echo.
    echo Please run 'dotnet publish src/LightHashTab/LightHashTab.csproj -c Release -r win-x64' first.
    echo.
    pause
    exit /b 1
)

echo Registering COM DLL:
echo "%DLL_PATH%"
echo.

regsvr32.exe "%DLL_PATH%"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] lightHashTab installed and registered successfully!
    echo Right-click any file in Windows Explorer and open Properties to see the 'File Hashes' tab.
) else (
    echo.
    echo [ERROR] Registration failed. Please make sure you ran this script as Administrator.
)

echo.
pause
