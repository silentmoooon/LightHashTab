# lightHashTab

A lightweight, high-performance, modern Windows Explorer Property Sheet Shell Extension built in **C# (.NET NativeAOT)**, inspired by [OpenHashTab](https://github.com/namazso/OpenHashTab).

It embeds a **"File Hashes"** tab directly into Windows Explorer's file Properties dialog, offering rapid multi-algorithm hash calculation (including **BLAKE3**), instant hash comparison, clipboard copying, and dark mode support with an ultra-low memory and disk footprint (~1.8 MB standalone unmanaged DLL).

![LightHashTab Screenshot](assets/screenshot.png)

---

## ✨ Features

- **🚀 NativeAOT Unmanaged Shell Extension**: Compiles directly to a standalone native dynamic link library (`LightHashTab.dll`) with zero CoreCLR host overhead and fast startup (<10ms).
- **⚡ High-Performance Multi-Hashing**:
  - **BLAKE3** (Managed SIMD hardware-accelerated with AVX2/SSE)
  - **SHA-256**, **SHA-512**, **SHA-1**, **MD5** (.NET hardware-accelerated cryptography)
  - **CRC32**, **XXH64** (`System.IO.Hashing`)
  - Single-pass chunked streaming I/O — file is read only once.
- **🔍 Live Hash Matching**: Paste any checksum into the comparison box to instantly highlight the matching algorithm (✔ Match / ✖ No match).
- **📋 One-Click Copying**: Copy single hash values or export formatted summaries to clipboard.
- **🎨 Native Explorer UI & Dark Mode**: Lightweight Win32 controls (`SysListView32`, `msctls_progress32`) styled with Windows Explorer themes (`uxtheme`).

---

## 🛠️ Building from Source

### Prerequisites
- .NET 10 (or .NET 8 / 9) SDK
- Visual Studio C++ Build Tools (MSVC Linker for NativeAOT)

### Build & Publish
```powershell
dotnet publish src/LightHashTab/LightHashTab.csproj -c Release -r win-x64
```
The compiled standalone DLL will be located at:
`src/LightHashTab/bin/Release/net10.0-windows/win-x64/publish/LightHashTab.dll`

---

## 📦 Installation / Registration

Run Command Prompt or PowerShell as **Administrator**:

### Register
```cmd
regsvr32.exe "<path-to>\LightHashTab.dll"
```

### Unregister
```cmd
regsvr32.exe /u "<path-to>\LightHashTab.dll"
```

---

## 🧪 Testing

Run automated tests:
```powershell
dotnet test
```
