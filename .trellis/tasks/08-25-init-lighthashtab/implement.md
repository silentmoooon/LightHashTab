# Implementation Plan: lightHashTab

## Implementation Steps

### Step 1: Project Setup & NativeAOT Shared Library Configuration
- Create `src/LightHashTab/LightHashTab.csproj` targeting `net10.0` (or `net8.0`/`net9.0`) with `<PublishAot>true</PublishAot>` and `<NativeLib>Shared</NativeLib>`.
- Add packages: `Blake3` (managed SIMD version), `System.IO.Hashing`.
- Verify build & NativeAOT publishing toolchain (`dotnet publish -c Release -r win-x64`).

### Step 2: Win32 P/Invoke & COM VTable Foundations
- Create `Interop/Win32.cs`:
  - Enums, structs (`PROPSHEETPAGEW`, `DLGTEMPLATE`, `LVCOLUMNW`, `LVITEMW`, `RECT`, `POINT`, `MSG`, etc.).
  - P/Invoke definitions (`user32.dll`, `kernel32.dll`, `comctl32.dll`, `uxtheme.dll`, `shell32.dll`, `ole32.dll`, `advapi32.dll`).
- Create COM definitions & VTables (`Interop/Com.cs`, `Interop/VTables.cs`):
  - `IUnknown`, `IClassFactory`, `IShellExtInit`, `IShellPropSheetExt`.
  - Object memory wrappers and reference counting.

### Step 3: DLL Exports & COM Registration
- Implement `Exports.cs`:
  - `DllGetClassObject`: Creates `IClassFactory` returning our `ShellExtension` instance.
  - `DllCanUnloadNow`: Tracks active reference count.
  - `DllRegisterServer` / `DllUnregisterServer`: Sets up registry keys under `HKCR\*\shellex\PropertySheetHandlers\LightHashTab` and `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved`.

### Step 4: High-Performance Hash Engine
- Create `Hashing/HashEngine.cs`:
  - Single-pass chunked file reader (`FileStream` with 1MB chunk buffer).
  - Pipeline support for BLAKE3, SHA-256, SHA-512, SHA-1, MD5, CRC32, XXH64.
  - Async execution with progress reporting & cancellation token support.

### Step 5: Win32 Property Sheet Page & UI Controls
- Create `UI/PropertySheetPage.cs`:
  - In-memory `DLGTEMPLATE` setup.
  - Unmanaged `DialogProc` handling messages (`WM_INITDIALOG`, `WM_SIZE`, `WM_COMMAND`, `WM_NOTIFY`, `WM_DESTROY`, `WM_USER+...`).
  - Child controls creation: `SysListView32` (hash results table), `Edit` (comparison input), `msctls_progress32` (progress bar), buttons ("Copy All", "Copy Selected").
  - Dark mode and modern styling via `uxtheme!SetWindowTheme`.
  - Hash matching & real-time visual feedback.

### Step 6: End-to-End Build & Verification
- Compile and publish NativeAOT unmanaged DLL (`LightHashTab.dll`).
- Test COM registration via `regsvr32`.
- Verify Property Sheet tab in Windows Explorer, hash accuracy, comparison, and copy functionality.
- Test unregistration via `regsvr32 /u`.
