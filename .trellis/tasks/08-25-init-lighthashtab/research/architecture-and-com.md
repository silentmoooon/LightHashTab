# Technical Research: COM PropertySheet Extension with .NET NativeAOT

## 1. NativeAOT COM DLL Export Architecture

In .NET 8/9/10 with `<PublishAot>true</PublishAot>` and `<NativeLib>Shared</NativeLib>`, C# compiles into a standalone native unmanaged DLL.
Native COM entry points are exported via `[UnmanagedCallersOnly]`:

```csharp
[UnmanagedCallersOnly(EntryPoint = "DllGetClassObject")]
public static unsafe int DllGetClassObject(Guid* rclsid, Guid* riid, void** ppv)

[UnmanagedCallersOnly(EntryPoint = "DllCanUnloadNow")]
public static int DllCanUnloadNow()

[UnmanagedCallersOnly(EntryPoint = "DllRegisterServer")]
public static int DllRegisterServer()

[UnmanagedCallersOnly(EntryPoint = "DllUnregisterServer")]
public static int DllUnregisterServer()
```

## 2. COM Interfaces & VTables

For high reliability and zero runtime marshalling overhead in NativeAOT, COM interfaces can be implemented using C#-struct VTables with `delegate* unmanaged[Stdcall]` function pointers.

### IShellExtInit (`000214e8-0000-0000-c000-000000000046`)
- `Initialize(PCIDLIST_ABSOLUTE pidlFolder, IDataObject* pdtobj, HKEY hkeyProgID)`
- Extracts files via `pdtobj->GetData(&formatetc, &stgmedium)` with `CF_HDROP`.
- Uses `DragQueryFileW` to retrieve all selected file paths.

### IShellPropSheetExt (`000214e9-0000-0000-c000-000000000046`)
- `AddPages(LPFNADDPROPSHEETPAGE pfnAddPage, LPARAM lParam)`
- Builds a `PROPSHEETPAGEW` struct with `PSP_DLGINDIRECT | PSP_USETITLE`.
- Uses an in-memory `DLGTEMPLATE` (no `.rc` compilation needed).
- Invokes `CreatePropertySheetPageW(&psp)` to get `HPROPSHEETPAGE`.
- Calls `pfnAddPage(hPage, lParam)` to embed the tab into Explorer's properties dialog.
- `ReplacePage(...)`: returns `E_NOTIMPL`.

## 3. Win32 UI in Explorer Dialog

- **Template**: `DLGTEMPLATE` in memory with `WS_CHILD | WS_VISIBLE | DS_CONTROL`.
- **DialogProc**: handles `WM_INITDIALOG`, `WM_SIZE`, `WM_COMMAND`, `WM_NOTIFY`, `WM_DESTROY`.
- **Controls**:
  - `SysListView32`: Displays hash algorithm names, calculated hash values, and match status.
  - `Edit`: Comparison input box (auto-checks against calculated hashes).
  - `msctls_progress32`: Calculation progress.
  - Action buttons: Copy All, Copy Selected, Settings.
- **Theming**: Supports Dark Mode (`uxtheme!SetWindowTheme`, `AllowDarkModeForWindow`, `SetPreferredAppMode`).

## 4. Hashing Pipeline

- **Algorithms**:
  - `Blake3` (Pure managed SIMD implementation with AVX2/SSE/NEON support).
  - `System.Security.Cryptography`: SHA256, SHA512, SHA1, MD5.
  - `System.IO.Hashing`: CRC32, XxHash64, XxHash128.
- **I/O & Concurrency**:
  - Single-pass streaming read (`1MB` chunk buffer) with `FileShare.ReadWrite | FileShare.Delete`.
  - Parallel block feed to all active hashers.
  - Asynchronous background calculation with progress reporting to UI via `PostMessageW`.
  - Thread-safe cancellation on dialog exit or tab switch.
