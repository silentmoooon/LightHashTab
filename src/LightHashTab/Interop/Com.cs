using System;
using System.Runtime.InteropServices;

namespace LightHashTab.Interop;

public static unsafe class Com
{
    // HRESULT Constants
    public const int S_OK = 0;
    public const int S_FALSE = 1;
    public const int E_FAIL = unchecked((int)0x80004005);
    public const int E_INVALIDARG = unchecked((int)0x80070057);
    public const int E_NOINTERFACE = unchecked((int)0x80004002);
    public const int E_POINTER = unchecked((int)0x80004003);
    public const int E_NOTIMPL = unchecked((int)0x80004001);
    public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
    public const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
    public const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);

    // Well-known COM GUIDs
    public static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    public static readonly Guid IID_IClassFactory = new("00000001-0000-0000-C000-000000000046");
    public static readonly Guid IID_IDataObject = new("0000010e-0000-0000-C000-000000000046");
    public static readonly Guid IID_IShellExtInit = new("000214e8-0000-0000-c000-000000000046");
    public static readonly Guid IID_IShellPropSheetExt = new("000214e9-0000-0000-c000-000000000046");

    // Extension Class GUID (LightHashTab Shell Extension)
    public static readonly Guid CLSID_LightHashTab = new("D6B58F2E-9C64-428E-81C5-A5D80F74A1B2");
    public const string CLSID_LightHashTab_String = "{D6B58F2E-9C64-428E-81C5-A5D80F74A1B2}";
    public const string ExtensionDisplayName = "LightHashTab Property Sheet Extension";

    [DllImport("ole32.dll", ExactSpelling = true)]
    public static extern void ReleaseStgMedium(STGMEDIUM* pmedium);
}

[StructLayout(LayoutKind.Sequential)]
public struct FORMATETC
{
    public ushort cfFormat;
    public nint ptd;
    public uint dwAspect;
    public int lindex;
    public uint tymed;
}

[StructLayout(LayoutKind.Sequential)]
public struct STGMEDIUM
{
    public uint tymed;
    public nint hGlobal;
    public nint pUnkForRelease;
}

public enum TYMED : uint
{
    TYMED_HGLOBAL = 1,
    TYMED_FILE = 2,
    TYMED_ISTREAM = 4,
    TYMED_ISTORAGE = 8,
    TYMED_GDI = 16,
    TYMED_MFPICT = 32,
    TYMED_ENHMF = 64,
    TYMED_NULL = 0
}

public enum DVASPECT : uint
{
    DVASPECT_CONTENT = 1,
    DVASPECT_THUMBNAIL = 2,
    DVASPECT_ICON = 4,
    DVASPECT_DOCPRINT = 8
}
