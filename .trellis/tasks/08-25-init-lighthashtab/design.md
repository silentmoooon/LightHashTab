# Technical Design: lightHashTab

## 1. Architecture Overview

`lightHashTab` is structured into four main layers:

```
+-------------------------------------------------------------------+
|                    Windows Explorer (explorer.exe)                |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 1. COM Shell Extension Layer (Exports & Interfaces)               |
|    - DllGetClassObject, DllCanUnloadNow, DllRegisterServer        |
|    - IClassFactory, IShellExtInit, IShellPropSheetExt             |
|    - Manual VTable generation compatible with NativeAOT           |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 2. Win32 UI Layer (Property Sheet Page)                           |
|    - In-Memory DLGTEMPLATE & PROPSHEETPAGEW registration          |
|    - DlgProc message loop handling (WM_INITDIALOG, WM_SIZE, etc.) |
|    - SysListView32 (Hash Grid), Edit (Compare), Progress Bar      |
|    - Dark Mode (uxtheme) & DPI scaling                            |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 3. Hashing Pipeline & Engine                                      |
|    - Single-pass chunked stream reader (1MB block buffer)         |
|    - BLAKE3 (SIMD managed), SHA-256, SHA-512, MD5, CRC32, XXH64   |
|    - Background Task execution & CancellationToken               |
|    - Progress & UI post-messaging (PostMessageW)                  |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
| 4. System / Registry Utilities                                    |
|    - Registration in HKCR\*\shellex\PropertySheetHandlers         |
|    - HKLM Shell Extensions Approved registry keys                 |
+-------------------------------------------------------------------+
```

## 2. Component Design Details

### A. COM & NativeAOT Interface Layer
- **CLSID**: Unique GUID for `LightHashTab` (e.g. `{D6B58F2E-9C64-428E-81C5-A5D80F74A1B2}`).
- **COM VTable Implementation**:
  - `IUnknownVTable`: `QueryInterface`, `AddRef`, `Release`.
  - `IShellExtInitVTable`: `Initialize(PCIDLIST_ABSOLUTE, IDataObject*, HKEY)`.
  - `IShellPropSheetExtVTable`: `AddPages(LPFNADDPROPSHEETPAGE, LPARAM)`, `ReplacePage(...)`.
  - `IClassFactoryVTable`: `CreateInstance`, `LockServer`.
  - Memory instances are allocated via unmanaged heap (`NativeMemory.Alloc`) or pinned structs to ensure zero-GC pinning hazards.

### B. Shell Initialization & Data Extraction
- `IShellExtInit.Initialize`:
  - Interrogates `IDataObject` for `CF_HDROP`.
  - Calls `DragQueryFileW` to enumerate all dropped/selected file paths.
  - Passes the file list to the PropertySheetPage context.

### C. Win32 Property Sheet Page & UI
- **In-Memory Dialog Template**:
  - `DLGTEMPLATE` struct with `WS_CHILD | WS_VISIBLE | DS_CONTROL`.
- **Property Page Entry**:
  - `CreatePropertySheetPageW` with `PROPSHEETPAGEW` (`pszTitle = "File Hashes" / "哈希校验"`).
  - Registers `pfnDlgProc` pointing to unmanaged export callback.
- **Controls & Layout**:
  - Top: File name / size summary, Settings button.
  - Center: `SysListView32` (Report view with 3 columns: Algorithm, Hash, Status).
  - Bottom: Comparison Edit box + Match status icon/text + Progress bar + Copy buttons.

### D. Hashing Engine
- Runs on .NET `ThreadPool` via `Task.Run`.
- Algorithms initialized in parallel:
  - `Blake3`: `Blake3.Hasher.New()`
  - `SHA256`: `IncrementalHash.CreateHash(HashAlgorithmName.SHA256)`
  - `SHA512`: `IncrementalHash.CreateHash(HashAlgorithmName.SHA512)`
  - `SHA1`: `IncrementalHash.CreateHash(HashAlgorithmName.SHA1)`
  - `MD5`: `IncrementalHash.CreateHash(HashAlgorithmName.MD5)`
  - `CRC32`: `System.IO.Hashing.Crc32`
  - `XXH64`: `System.IO.Hashing.XxHash64`
- Read Loop:
  - 1MB buffer read from `FileStream`.
  - Update all active hashers with read slice.
  - Progress calculation: `(bytesRead / totalBytes) * 100`.
  - Send UI progress update via `PostMessageW(hwnd, WM_APP_PROGRESS, percent, 0)`.
- Completion:
  - Format hash outputs (hex lowercase / uppercase).
  - `PostMessageW(hwnd, WM_APP_HASH_COMPLETED, ...)` to fill listview.

## 3. Project Configuration
- `<TargetFramework>net10.0</TargetFramework>` (or `net8.0`/`net9.0`)
- `<PublishAot>true</PublishAot>`
- `<NativeLib>Shared</NativeLib>`
- `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`
- `<StripSymbols>true</StripSymbols>`
- `<OptimizationPreference>Speed</OptimizationPreference>`
