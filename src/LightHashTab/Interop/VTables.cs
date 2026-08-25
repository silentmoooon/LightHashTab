using System;
using System.Runtime.InteropServices;

namespace LightHashTab.Interop;

public static unsafe class VTables
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IUnknownVTable
    {
        public delegate* unmanaged[Stdcall]<nint, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[Stdcall]<nint, uint> AddRef;
        public delegate* unmanaged[Stdcall]<nint, uint> Release;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IClassFactoryVTable
    {
        public delegate* unmanaged[Stdcall]<nint, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[Stdcall]<nint, uint> AddRef;
        public delegate* unmanaged[Stdcall]<nint, uint> Release;
        public delegate* unmanaged[Stdcall]<nint, nint, Guid*, void**, int> CreateInstance;
        public delegate* unmanaged[Stdcall]<nint, int, int> LockServer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IShellExtInitVTable
    {
        public delegate* unmanaged[Stdcall]<nint, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[Stdcall]<nint, uint> AddRef;
        public delegate* unmanaged[Stdcall]<nint, uint> Release;
        public delegate* unmanaged[Stdcall]<nint, nint, nint, nint, int> Initialize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IShellPropSheetExtVTable
    {
        public delegate* unmanaged[Stdcall]<nint, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[Stdcall]<nint, uint> AddRef;
        public delegate* unmanaged[Stdcall]<nint, uint> Release;
        public delegate* unmanaged[Stdcall]<nint, delegate* unmanaged[Stdcall]<nint, nint, int>, nint, int> AddPages;
        public delegate* unmanaged[Stdcall]<nint, uint, delegate* unmanaged[Stdcall]<nint, nint, int>, nint, int> ReplacePage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IDataObjectVTable
    {
        public delegate* unmanaged[Stdcall]<nint, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[Stdcall]<nint, uint> AddRef;
        public delegate* unmanaged[Stdcall]<nint, uint> Release;
        public delegate* unmanaged[Stdcall]<nint, FORMATETC*, STGMEDIUM*, int> GetData;
    }
}
