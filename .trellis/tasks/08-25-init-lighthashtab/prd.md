# PRD: lightHashTab - C# NativeAOT Property Sheet Extension

## 1. Goal & Value
Build a lightweight, high-performance, modern Windows Explorer Shell Extension in C# (.NET 10/9/8 NativeAOT) inspired by OpenHashTab.
It integrates a "File Hashes" (文件校验) tab into the Windows Explorer File Properties dialog, enabling instant multi-algorithm hash calculation (including BLAKE3), dynamic hash matching/comparison, one-click copying, and dark mode support with minimal memory and disk footprint.

## 2. Requirements & Features

### Core Requirements
1. **Windows Explorer Property Sheet Tab Integration**:
   - Implements COM `IShellExtInit` and `IShellPropSheetExt` interfaces.
   - Registers for all file types (`HKCR\*\shellex\PropertySheetHandlers\LightHashTab`).
   - Supports single file and multiple file selection.
2. **NativeAOT Standalone Unmanaged DLL**:
   - Pure standalone unmanaged DLL with zero dependency on external CoreCLR runtime.
   - Exports standard COM entry points: `DllGetClassObject`, `DllCanUnloadNow`, `DllRegisterServer`, `DllUnregisterServer`.
   - Fast initialization (<10ms) and low memory usage.
3. **High-Performance Hashing Engine**:
   - Default algorithms: **BLAKE3**, **SHA-256**, **SHA-512**, **SHA-1**, **MD5**, **CRC32**, **XXH64**.
   - Single-pass I/O streaming (read chunk once, fan out to all enabled algorithms).
   - Multi-threaded background processing so Explorer UI remains fully responsive.
   - Support cancelation when dialog closes or user switches selection.
4. **Interactive Win32 UI in Property Sheet**:
   - `SysListView32` grid displaying Algorithm Name, Hash Value, and Match status.
   - Live progress bar indicating hash calculation status.
   - Hash Comparison / Match Input box: pasting a hash string automatically highlights matching algorithms (green checkmark / red cross).
   - Quick action buttons & Context Menu: "Copy Selected", "Copy All", "Export to File".
   - Dark mode support matching Windows 10/11 system theme.

## 3. Out of Scope (MVP)
- VirusTotal API lookup integration (can be added in v2).
- Shell context menu (right click submenu) - MVP focuses purely on the Property Sheet extension tab.

## 4. Acceptance Criteria
- [ ] Successfully compiles to native DLL via `dotnet publish -c Release -r win-x64`.
- [ ] `regsvr32 LightHashTab.dll` registers the COM extension without errors.
- [ ] Right-clicking any file in Windows Explorer -> "Properties" -> displays "File Hashes" (哈希值) tab.
- [ ] Tab computes BLAKE3, SHA-256, SHA-1, MD5, CRC32 accurately and displays progress.
- [ ] Pasting a known hash into the comparison box matches and highlights the corresponding row.
- [ ] "Copy" actions copy the expected hash values to clipboard.
- [ ] `regsvr32 /u LightHashTab.dll` cleanly removes registry entries.
