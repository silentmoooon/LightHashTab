using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using LightHashTab.Interop;
using LightHashTab.UI;

namespace LightHashTab.Shell;

public sealed class ShellExtensionState
{
    public List<string> FilePaths { get; } = [];
}

public static unsafe class PropertySheetExtension
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShellExtensionCOM
    {
        public VTables.IShellExtInitVTable* lpVtblShellExtInit;           // Offset 0
        public VTables.IShellPropSheetExtVTable* lpVtblShellPropSheetExt; // Offset 8
        public int refCount;
        public GCHandle stateHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClassFactoryCOM
    {
        public VTables.IClassFactoryVTable* lpVtbl;
        public int refCount;
    }

    private static readonly VTables.IShellExtInitVTable s_shellExtInitVTable;
    private static readonly VTables.IShellPropSheetExtVTable s_shellPropSheetExtVTable;
    private static readonly VTables.IClassFactoryVTable s_classFactoryVTable;

    private static readonly VTables.IShellExtInitVTable* s_pShellExtInitVTable;
    private static readonly VTables.IShellPropSheetExtVTable* s_pShellPropSheetExtVTable;
    private static readonly VTables.IClassFactoryVTable* s_pClassFactoryVTable;

    static PropertySheetExtension()
    {
        // 1. IShellExtInit VTable
        s_shellExtInitVTable = new VTables.IShellExtInitVTable
        {
            QueryInterface = &ShellExtInit_QueryInterface,
            AddRef = &ShellExtInit_AddRef,
            Release = &ShellExtInit_Release,
            Initialize = &ShellExtInit_Initialize
        };
        s_pShellExtInitVTable = (VTables.IShellExtInitVTable*)NativeMemory.Alloc((nuint)sizeof(VTables.IShellExtInitVTable));
        *s_pShellExtInitVTable = s_shellExtInitVTable;

        // 2. IShellPropSheetExt VTable
        s_shellPropSheetExtVTable = new VTables.IShellPropSheetExtVTable
        {
            QueryInterface = &ShellPropSheetExt_QueryInterface,
            AddRef = &ShellPropSheetExt_AddRef,
            Release = &ShellPropSheetExt_Release,
            AddPages = &ShellPropSheetExt_AddPages,
            ReplacePage = &ShellPropSheetExt_ReplacePage
        };
        s_pShellPropSheetExtVTable = (VTables.IShellPropSheetExtVTable*)NativeMemory.Alloc((nuint)sizeof(VTables.IShellPropSheetExtVTable));
        *s_pShellPropSheetExtVTable = s_shellPropSheetExtVTable;

        // 3. IClassFactory VTable
        s_classFactoryVTable = new VTables.IClassFactoryVTable
        {
            QueryInterface = &ClassFactory_QueryInterface,
            AddRef = &ClassFactory_AddRef,
            Release = &ClassFactory_Release,
            CreateInstance = &ClassFactory_CreateInstance,
            LockServer = &ClassFactory_LockServer
        };
        s_pClassFactoryVTable = (VTables.IClassFactoryVTable*)NativeMemory.Alloc((nuint)sizeof(VTables.IClassFactoryVTable));
        *s_pClassFactoryVTable = s_classFactoryVTable;
    }

    public static nint CreateClassFactory()
    {
        ClassFactoryCOM* factory = (ClassFactoryCOM*)NativeMemory.AllocZeroed((nuint)sizeof(ClassFactoryCOM));
        factory->lpVtbl = s_pClassFactoryVTable;
        factory->refCount = 1;
        return (nint)factory;
    }

    public static ShellExtensionCOM* CreateShellExtension()
    {
        ShellExtensionCOM* inst = (ShellExtensionCOM*)NativeMemory.AllocZeroed((nuint)sizeof(ShellExtensionCOM));
        inst->lpVtblShellExtInit = s_pShellExtInitVTable;
        inst->lpVtblShellPropSheetExt = s_pShellPropSheetExtVTable;
        inst->refCount = 1;
        inst->stateHandle = GCHandle.Alloc(new ShellExtensionState());
        return inst;
    }

    // -------------------------------------------------------------
    // IShellExtInit Methods (Offset 0)
    // -------------------------------------------------------------

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ShellExtInit_QueryInterface(nint thisPtr, Guid* riid, void** ppvObject)
    {
        return ShellExtInit_QueryInterface_Core(thisPtr, riid, ppvObject);
    }

    public static int ShellExtInit_QueryInterface_Core(nint thisPtr, Guid* riid, void** ppvObject)
    {
        if (ppvObject == null || riid == null) return Com.E_POINTER;
        *ppvObject = null;

        ShellExtensionCOM* self = (ShellExtensionCOM*)thisPtr;

        if (*riid == Com.IID_IUnknown || *riid == Com.IID_IShellExtInit)
        {
            *ppvObject = (void*)self;
            ShellExtInit_AddRef_Core(thisPtr);
            return Com.S_OK;
        }

        if (*riid == Com.IID_IShellPropSheetExt)
        {
            *ppvObject = (void*)((byte*)self + sizeof(nint));
            ShellExtInit_AddRef_Core(thisPtr);
            return Com.S_OK;
        }

        return Com.E_NOINTERFACE;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint ShellExtInit_AddRef(nint thisPtr)
    {
        return ShellExtInit_AddRef_Core(thisPtr);
    }

    public static uint ShellExtInit_AddRef_Core(nint thisPtr)
    {
        ShellExtensionCOM* self = (ShellExtensionCOM*)thisPtr;
        return (uint)Interlocked.Increment(ref self->refCount);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint ShellExtInit_Release(nint thisPtr)
    {
        return ShellExtInit_Release_Core(thisPtr);
    }

    public static uint ShellExtInit_Release_Core(nint thisPtr)
    {
        ShellExtensionCOM* self = (ShellExtensionCOM*)thisPtr;
        int count = Interlocked.Decrement(ref self->refCount);
        if (count == 0)
        {
            if (self->stateHandle.IsAllocated)
            {
                self->stateHandle.Free();
            }
            NativeMemory.Free(self);
        }
        return (uint)count;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ShellExtInit_Initialize(nint thisPtr, nint pidlFolder, nint pdtobj, nint hkeyProgID)
    {
        ShellExtensionCOM* self = (ShellExtensionCOM*)thisPtr;
        if (!self->stateHandle.IsAllocated || self->stateHandle.Target is not ShellExtensionState state)
        {
            return Com.E_FAIL;
        }

        state.FilePaths.Clear();

        if (pdtobj != 0)
        {
            FORMATETC fmt = new()
            {
                cfFormat = (ushort)Win32.CF_HDROP,
                ptd = 0,
                dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = (uint)TYMED.TYMED_HGLOBAL
            };

            STGMEDIUM medium = default;
            VTables.IDataObjectVTable** pVtbl = (VTables.IDataObjectVTable**)pdtobj;
            int hr = (*pVtbl)->GetData(pdtobj, &fmt, &medium);

            if (hr == Com.S_OK && medium.hGlobal != 0)
            {
                nint hDrop = medium.hGlobal;
                uint count = Win32.DragQueryFileW(hDrop, 0xFFFFFFFF, null, 0);
                char* pBuf = stackalloc char[2048];

                for (uint i = 0; i < count; i++)
                {
                    uint len = Win32.DragQueryFileW(hDrop, i, pBuf, 2048);
                    if (len > 0)
                    {
                        state.FilePaths.Add(new string(pBuf, 0, (int)len));
                    }
                }

                Com.ReleaseStgMedium(&medium);
            }
        }

        return Com.S_OK;
    }

    // -------------------------------------------------------------
    // IShellPropSheetExt Methods (Offset 8)
    // -------------------------------------------------------------

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ShellPropSheetExt_QueryInterface(nint thisPtr, Guid* riid, void** ppvObject)
    {
        nint basePtr = (nint)((byte*)thisPtr - sizeof(nint));
        return ShellExtInit_QueryInterface_Core(basePtr, riid, ppvObject);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint ShellPropSheetExt_AddRef(nint thisPtr)
    {
        nint basePtr = (nint)((byte*)thisPtr - sizeof(nint));
        return ShellExtInit_AddRef_Core(basePtr);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint ShellPropSheetExt_Release(nint thisPtr)
    {
        nint basePtr = (nint)((byte*)thisPtr - sizeof(nint));
        return ShellExtInit_Release_Core(basePtr);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ShellPropSheetExt_AddPages(nint thisPtr, delegate* unmanaged[Stdcall]<nint, nint, int> pfnAddPage, nint lParam)
    {
        ShellExtensionCOM* self = (ShellExtensionCOM*)((byte*)thisPtr - sizeof(nint));
        if (!self->stateHandle.IsAllocated || self->stateHandle.Target is not ShellExtensionState state)
        {
            return Com.E_FAIL;
        }

        if (state.FilePaths.Count > 0)
        {
            nint hPage = PropertySheetPage.CreatePage(state.FilePaths);
            if (hPage != 0)
            {
                if (pfnAddPage(hPage, lParam) == 0)
                {
                    Win32.DestroyPropertySheetPage(hPage);
                    return Com.E_FAIL;
                }
            }
        }

        return Com.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ShellPropSheetExt_ReplacePage(nint thisPtr, uint uPageID, delegate* unmanaged[Stdcall]<nint, nint, int> pfnReplaceWith, nint lParam)
    {
        return Com.E_NOTIMPL;
    }

    // -------------------------------------------------------------
    // IClassFactory Methods
    // -------------------------------------------------------------

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ClassFactory_QueryInterface(nint thisPtr, Guid* riid, void** ppvObject)
    {
        return ClassFactory_QueryInterface_Core(thisPtr, riid, ppvObject);
    }

    public static int ClassFactory_QueryInterface_Core(nint thisPtr, Guid* riid, void** ppvObject)
    {
        if (ppvObject == null || riid == null) return Com.E_POINTER;
        *ppvObject = null;

        if (*riid == Com.IID_IUnknown || *riid == Com.IID_IClassFactory)
        {
            *ppvObject = (void*)thisPtr;
            ClassFactory_AddRef_Core(thisPtr);
            return Com.S_OK;
        }

        return Com.E_NOINTERFACE;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint ClassFactory_AddRef(nint thisPtr)
    {
        return ClassFactory_AddRef_Core(thisPtr);
    }

    public static uint ClassFactory_AddRef_Core(nint thisPtr)
    {
        ClassFactoryCOM* self = (ClassFactoryCOM*)thisPtr;
        return (uint)Interlocked.Increment(ref self->refCount);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static uint ClassFactory_Release(nint thisPtr)
    {
        return ClassFactory_Release_Core(thisPtr);
    }

    public static uint ClassFactory_Release_Core(nint thisPtr)
    {
        ClassFactoryCOM* self = (ClassFactoryCOM*)thisPtr;
        int count = Interlocked.Decrement(ref self->refCount);
        if (count == 0)
        {
            NativeMemory.Free(self);
        }
        return (uint)count;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ClassFactory_CreateInstance(nint thisPtr, nint pUnkOuter, Guid* riid, void** ppvObject)
    {
        if (ppvObject == null || riid == null) return Com.E_POINTER;
        *ppvObject = null;

        if (pUnkOuter != 0) return Com.CLASS_E_NOAGGREGATION;

        ShellExtensionCOM* instance = CreateShellExtension();
        int hr = ShellExtInit_QueryInterface_Core((nint)instance, riid, ppvObject);
        ShellExtInit_Release_Core((nint)instance);
        return hr;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static int ClassFactory_LockServer(nint thisPtr, int fLock)
    {
        return Com.S_OK;
    }
}
