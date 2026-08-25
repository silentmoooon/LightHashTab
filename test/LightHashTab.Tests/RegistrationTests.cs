using System;
using System.IO;
using System.Runtime.InteropServices;
using LightHashTab;
using LightHashTab.Interop;
using Microsoft.Win32;
using Xunit;

namespace LightHashTab.Tests;

public class RegistrationTests
{
    [Fact]
    public void TestDiagnostic()
    {
        // 1. Test GetCurrentDllPath
        string dllPath = Exports.GetCurrentDllPath();
        Assert.False(string.IsNullOrEmpty(dllPath), $"GetCurrentDllPath returned empty! LastWin32Error={Marshal.GetLastWin32Error()}");

        // 2. Test Registry
        try
        {
            string clsidStr = Com.CLSID_LightHashTab_String;
            using var clsidKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsidStr}");
            clsidKey.SetValue(string.Empty, Com.ExtensionDisplayName);
            using var inprocKey = clsidKey.CreateSubKey("InprocServer32");
            inprocKey.SetValue(string.Empty, dllPath);
            inprocKey.SetValue("ThreadingModel", "Apartment");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Registry test failed: {ex}");
        }
    }
}
