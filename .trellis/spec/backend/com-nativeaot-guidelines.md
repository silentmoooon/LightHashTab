# COM & NativeAOT Guidelines in lightHashTab

## 1. NativeAOT COM Export Conventions
- Shell extensions compiled with NativeAOT must not depend on external CoreCLR hosts.
- Unmanaged COM entry points must be decorated with `[UnmanagedCallersOnly(EntryPoint = "...", CallConvs = [typeof(CallConvStdcall)])]`.
- Direct invocations of `[UnmanagedCallersOnly]` methods from managed C# code are prohibited by CS8901. Separate business logic into `_Core` private/internal methods and forward from the unmanaged entry points.

## 2. In-Memory Win32 Property Sheet Templates
- Avoid compiling `.res` / `.rc` files whenever possible.
- Use `DLGTEMPLATE` packed structures in memory (`PSP_DLGINDIRECT`) with `WS_CHILD | WS_VISIBLE | DS_CONTROL`.
- Attach `GCHandle` representing the page state to `PROPSHEETPAGEW.lParam` and store it in `DWLP_USER` on `WM_INITDIALOG`.
- Free the `GCHandle` upon receiving `WM_DESTROY`.

## 3. High-Performance Hashing Pipeline
- Use single-pass file chunk streaming (`1MB` buffer) with `FileShare.ReadWrite | FileShare.Delete`.
- Feed the chunk to all active hashers concurrently in memory to avoid repeated disk reads.
- Always use thread-pool background execution and notify the Win32 dialog thread via `PostMessageW`.
