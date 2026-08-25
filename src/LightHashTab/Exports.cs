using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using LightHashTab.Interop;
using LightHashTab.Shell;

namespace LightHashTab;

public static unsafe class Exports
{
    public static int ServerLockCount = 0;

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetModuleHandleExW(uint dwFlags, void* lpModuleName, out nint phModule);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetModuleFileNameW(nint hModule, char* lpFilename, uint nSize);

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, nint dwItem1, nint dwItem2);

    private const uint GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x00000004;
    private const uint GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x00000002;
    private const uint SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;

    [UnmanagedCallersOnly(EntryPoint = "DllGetClassObject", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllGetClassObject(Guid* rclsid, Guid* riid, void** ppv)
    {
        if (rclsid == null || riid == null || ppv == null)
        {
            return Com.E_POINTER;
        }

        *ppv = null;

        if (*rclsid != Com.CLSID_LightHashTab)
        {
            return Com.CLASS_E_CLASSNOTAVAILABLE;
        }

        nint pFactory = PropertySheetExtension.CreateClassFactory();
        int hr = PropertySheetExtension.ClassFactory_QueryInterface_Core(pFactory, riid, ppv);
        PropertySheetExtension.ClassFactory_Release_Core(pFactory);
        return hr;
    }

    [UnmanagedCallersOnly(EntryPoint = "DllCanUnloadNow", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllCanUnloadNow()
    {
        return ServerLockCount == 0 ? Com.S_OK : Com.S_FALSE;
    }

    [UnmanagedCallersOnly(EntryPoint = "DllRegisterServer", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllRegisterServer()
    {
        try
        {
            string dllPath = GetCurrentDllPath();
            if (string.IsNullOrEmpty(dllPath))
            {
                return Com.E_FAIL;
            }

            string clsidStr = Com.CLSID_LightHashTab_String;

            // 1. Register CLSID: HKCR\CLSID\{CLSID}
            using (var clsidKey = Registry.ClassesRoot.CreateSubKey($@"CLSID\{clsidStr}"))
            {
                clsidKey.SetValue(string.Empty, Com.ExtensionDisplayName);
                using var inprocKey = clsidKey.CreateSubKey("InprocServer32");
                inprocKey.SetValue(string.Empty, dllPath);
                inprocKey.SetValue("ThreadingModel", "Apartment");
            }

            // 2. Register PropertySheet Handler for all files: HKCR\*\shellex\PropertySheetHandlers\LightHashTab
            using (var pshKey = Registry.ClassesRoot.CreateSubKey(@"*\shellex\PropertySheetHandlers\LightHashTab"))
            {
                pshKey.SetValue(string.Empty, clsidStr);
            }

            // 3. Register PropertySheet Handler for directories: HKCR\Directory\shellex\PropertySheetHandlers\LightHashTab
            using (var dirKey = Registry.ClassesRoot.CreateSubKey(@"Directory\shellex\PropertySheetHandlers\LightHashTab"))
            {
                dirKey.SetValue(string.Empty, clsidStr);
            }

            // 4. Register in Approved Shell Extensions list
            using (var approvedKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved"))
            {
                approvedKey.SetValue(clsidStr, Com.ExtensionDisplayName);
            }

            // Notify Explorer
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
            return Com.S_OK;
        }
        catch
        {
            return Com.E_FAIL;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "DllUnregisterServer", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllUnregisterServer()
    {
        try
        {
            string clsidStr = Com.CLSID_LightHashTab_String;

            // 1. Remove HKCR\*\shellex\PropertySheetHandlers\LightHashTab
            try { Registry.ClassesRoot.DeleteSubKeyTree(@"*\shellex\PropertySheetHandlers\LightHashTab", false); } catch { }

            // 2. Remove HKCR\Directory\shellex\PropertySheetHandlers\LightHashTab
            try { Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shellex\PropertySheetHandlers\LightHashTab", false); } catch { }

            // 3. Remove HKCR\CLSID\{CLSID}
            try { Registry.ClassesRoot.DeleteSubKeyTree($@"CLSID\{clsidStr}", false); } catch { }

            // 4. Remove from Approved list
            try
            {
                using var approvedKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", true);
                approvedKey?.DeleteValue(clsidStr, false);
            }
            catch { }

            // Notify Explorer
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
            return Com.S_OK;
        }
        catch
        {
            return Com.E_FAIL;
        }
    }

    private static string GetCurrentDllPath()
    {
        delegate* unmanaged[Stdcall]<int> pFunc = &DllRegisterServer;
        if (GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, (void*)pFunc, out nint hModule))
        {
            char* pBuf = stackalloc char[2048];
            uint len = GetModuleFileNameW(hModule, pBuf, 2048);
            if (len > 0)
            {
                return new string(pBuf, 0, (int)len);
            }
        }
        return string.Empty;
    }
}
