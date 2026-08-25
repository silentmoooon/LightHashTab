# make-release.ps1
# Build LightHashTab and package it for distribution.
# Outputs to dist/ (never touches the registered publish/ DLL).
# Usage: .\make-release.ps1 [-Version "1.0.0"]

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root       = $PSScriptRoot
$distDir    = "$root\dist"
$buildOut   = "$distDir\_build"          # NativeAOT output goes HERE, not publish/
$packageDir = "$distDir\LightHashTab-$Version"

Write-Host "=== LightHashTab Release Builder ===" -ForegroundColor Cyan
Write-Host "Version : $Version"
Write-Host "Output  : $distDir"
Write-Host ""

# 1. Build to dist/_build  (avoids locking the registered publish/ DLL)
Write-Host "[1/3] Building NativeAOT (win-x64)..." -ForegroundColor Yellow

if (Test-Path $buildOut) { Remove-Item $buildOut -Recurse -Force }

dotnet publish "$root\src\LightHashTab\LightHashTab.csproj" `
    -c Release -r win-x64 `
    --output "$buildOut" `
    -p:Version=$Version

if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$dll = "$buildOut\LightHashTab.dll"
if (-not (Test-Path $dll)) { throw "DLL not found in build output." }

# 2. Stage package
Write-Host ""
Write-Host "[2/3] Staging package..." -ForegroundColor Yellow

if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir | Out-Null

Copy-Item $dll                           "$packageDir\"
Copy-Item "$root\setup\install.cmd"      "$packageDir\"
Copy-Item "$root\setup\uninstall.cmd"    "$packageDir\"

@"
LightHashTab $Version
====================
A lightweight file hashing shell extension for Windows Explorer.

Supported algorithms: BLAKE3, SHA-256, SHA-512, SHA-1, MD5, CRC32, XXH64

INSTALL
-------
1. Double-click install.cmd  (no admin required)
2. Right-click any file > Properties > "File Hashes" tab

REQUIREMENTS
------------
- Windows 10 or later (x64)
- No .NET runtime required

UNINSTALL
---------
Double-click uninstall.cmd

SOURCE
------
https://github.com/your-username/lightHashTab
"@ | Set-Content "$packageDir\README.txt" -Encoding UTF8

# 3. Zip
Write-Host "[3/3] Creating zip..." -ForegroundColor Yellow
$zipPath = "$distDir\LightHashTab-$Version-win-x64.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path "$packageDir\*" -DestinationPath $zipPath

# Cleanup temp build dir
Remove-Item $buildOut -Recurse -Force

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
Write-Host ""
Write-Host "Release zip : $zipPath"
$size = (Get-Item $zipPath).Length / 1MB
Write-Host ("Size        : {0:0.0} MB" -f $size)
Write-Host ""
Write-Host "Package contents:"
Get-ChildItem $packageDir | Format-Table Name, @{N="Size(KB)";E={[math]::Round($_.Length/1KB,1)}} -AutoSize
