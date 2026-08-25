using System;
using System.Runtime.InteropServices;

namespace LightHashTab.Interop;

public static unsafe class Win32
{
    // Window Styles
    public const uint WS_CHILD = 0x40000000;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_CLIPSIBLINGS = 0x04000000;
    public const uint WS_CLIPCHILDREN = 0x02000000;
    public const uint WS_TABSTOP = 0x00010000;
    public const uint WS_BORDER = 0x00800000;
    public const uint DS_CONTROL = 0x00000400;

    // Window Messages
    public const uint WM_NULL = 0x0000;
    public const uint WM_CREATE = 0x0001;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_SIZE = 0x0005;
    public const uint WM_SETTEXT = 0x000C;
    public const uint WM_GETTEXT = 0x000D;
    public const uint WM_GETTEXTLENGTH = 0x000E;
    public const uint WM_SETFONT = 0x0030;
    public const uint WM_GETFONT = 0x0031;
    public const uint WM_NOTIFY = 0x004E;
    public const uint WM_INITDIALOG = 0x0110;
    public const uint WM_COMMAND = 0x0111;
    public const uint WM_CTLCOLORDLG = 0x0146;
    public const uint WM_CTLCOLORSTATIC = 0x0148;
    public const uint WM_CTLCOLOREDIT = 0x0143;
    public const uint WM_CTLCOLORBTN = 0x0145;
    public const uint WM_THEMECHANGED = 0x031A;
    public const uint WM_USER = 0x0400;
    public const uint WM_APP = 0x8000;

    // Custom App Messages
    public const uint WM_APP_PROGRESS = WM_APP + 1;
    public const uint WM_APP_HASH_RESULT = WM_APP + 2;
    public const uint WM_APP_HASH_FINISHED = WM_APP + 3;

    // Property Sheet Flags & Notifications
    public const uint PSP_DEFAULT = 0x00000000;
    public const uint PSP_DLGINDIRECT = 0x00000001;
    public const uint PSP_USEHICON = 0x00000002;
    public const uint PSP_USEICONID = 0x00000004;
    public const uint PSP_USETITLE = 0x00000008;
    public const uint PSP_RTLREADING = 0x00000010;
    public const uint PSP_HASHELP = 0x00000020;
    public const uint PSP_USEREFPARENT = 0x00000040;
    public const uint PSP_USECALLBACK = 0x00000080;
    public const uint PSP_PREMATURE = 0x00000400;
    public const uint PSP_HIDEHEADER = 0x00000800;
    public const uint PSP_USEHEADERTITLE = 0x00001000;
    public const uint PSP_USEHEADERSUBTITLE = 0x00002000;

    public const uint PSN_FIRST = unchecked((uint)-200);
    public const uint PSN_SETACTIVE = PSN_FIRST - 0;
    public const uint PSN_KILLACTIVE = PSN_FIRST - 1;
    public const uint PSN_APPLY = PSN_FIRST - 2;
    public const uint PSN_RESET = PSN_FIRST - 3;

    // ListView Styles & Messages
    public const uint LVS_REPORT = 0x0001;
    public const uint LVS_SINGLESEL = 0x0004;
    public const uint LVS_SHOWSELALWAYS = 0x0008;
    public const uint LVS_ALIGNLEFT = 0x0800;
    public const uint LVS_OWNERDATA = 0x1000;
    public const uint LVS_NOSORTHEADER = 0x8000;

    public const uint LVM_FIRST = 0x1000;
    public const uint LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
    public const uint LVM_INSERTCOLUMNW = LVM_FIRST + 97;
    public const uint LVM_INSERTITEMW = LVM_FIRST + 77;
    public const uint LVM_SETITEMW = LVM_FIRST + 76;
    public const uint LVM_GETITEMTEXTW = LVM_FIRST + 115;
    public const uint LVM_SETITEMCOUNT = LVM_FIRST + 47;
    public const uint LVM_GETNEXTITEM = LVM_FIRST + 12;
    public const uint LVM_DELETEALLITEMS = LVM_FIRST + 9;
    public const uint LVM_SETCOLUMNWIDTH = LVM_FIRST + 30;

    public const uint LVS_EX_FULLROWSELECT = 0x00000020;
    public const uint LVS_EX_GRIDLINES = 0x00000001;
    public const uint LVS_EX_DOUBLEBUFFER = 0x00010000;

    public const uint LVCF_FMT = 0x0001;
    public const uint LVCF_WIDTH = 0x0002;
    public const uint LVCF_TEXT = 0x0004;
    public const uint LVCF_SUBITEM = 0x0008;
    public const int LVCFMT_LEFT = 0x0000;

    public const uint LVIF_TEXT = 0x0001;
    public const uint LVIF_IMAGE = 0x0002;
    public const uint LVIF_PARAM = 0x0004;
    public const uint LVIF_STATE = 0x0008;

    public const int LVNI_SELECTED = 0x0002;

    // Edit & Button Styles
    public const uint ES_LEFT = 0x0000;
    public const uint ES_AUTOHSCROLL = 0x0080;
    public const uint ES_READONLY = 0x0800;

    public const uint BS_PUSHBUTTON = 0x00000000;
    public const uint BS_DEFPUSHBUTTON = 0x00000001;

    // Progress Bar Messages
    public const uint PBM_SETRANGE32 = WM_USER + 6;
    public const uint PBM_SETPOS = WM_USER + 2;
    public const uint PBM_SETSTATE = WM_USER + 16;
    public const uint PBS_SMOOTH = 0x01;

    // Common Controls
    public const uint ICC_LISTVIEW_CLASSES = 0x00000001;
    public const uint ICC_PROGRESS_CLASS = 0x00000020;
    public const uint ICC_STANDARD_CLASSES = 0x00004000;

    // Clipboard
    public const uint CF_TEXT = 1;
    public const uint CF_UNICODETEXT = 13;
    public const uint CF_HDROP = 15;
    public const uint GMEM_MOVEABLE = 0x0002;

    // Windows API Functions
    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern nint CreateWindowExW(
        uint dwExStyle,
        [MarshalAs(UnmanagedType.LPWStr)] string? lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)] string? lpWindowName,
        uint dwStyle,
        int X,
        int Y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        void* lpParam);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint SendMessageW(nint hWnd, uint Msg, nuint wParam, nint lParam);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessageW(nint hWnd, uint Msg, nuint wParam, nint lParam);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowTextW(nint hWnd, [MarshalAs(UnmanagedType.LPWStr)] string lpString);

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextW(nint hWnd, char* lpString, int nMaxCount);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern int GetWindowTextLengthW(nint hWnd);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenClipboard(nint hWndNewOwner);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint SetClipboardData(uint uFormat, nint hMem);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    public static extern nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    public static extern void* GlobalLock(nint hMem);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalUnlock(nint hMem);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    public static extern nint GlobalFree(nint hMem);

    [DllImport("shell32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern uint DragQueryFileW(nint hDrop, uint iFile, char* lpszFile, uint cch);

    [DllImport("shell32.dll", ExactSpelling = true)]
    public static extern void DragFinish(nint hDrop);

    [DllImport("comctl32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern nint CreatePropertySheetPageW(PROPSHEETPAGEW* constPropSheetPagePointer);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyPropertySheetPage(nint hPropSheetPage);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InitCommonControlsEx(INITCOMMONCONTROLSEX* icc);

    [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern int SetWindowTheme(nint hWnd, [MarshalAs(UnmanagedType.LPWStr)] string? pszSubAppName, [MarshalAs(UnmanagedType.LPWStr)] string? pszSubIdList);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    public static extern nint GetWindowLongPtrW(nint hWnd, int nIndex);

    public const int DWLP_USER = 8;
    public const int GWLP_USERDATA = -21;
    public const int DWLP_MSGRESULT = 0;

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern nint GetStockObject(int i);
    public const int DEFAULT_GUI_FONT = 17;

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern uint SetTextColor(nint hdc, uint color);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern uint SetBkColor(nint hdc, uint color);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern int SetBkMode(nint hdc, int mode);
    public const int TRANSPARENT = 1;
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct INITCOMMONCONTROLSEX
{
    public uint dwSize;
    public uint dwICC;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct PROPSHEETPAGEW
{
    public uint dwSize;
    public uint dwFlags;
    public nint hInstance;
    public char* pszTemplate; // or DLGTEMPLATE* if PSP_DLGINDIRECT
    public nint hIcon;
    public char* pszTitle;
    public delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> pfnDlgProc;
    public nint lParam;
    public delegate* unmanaged[Stdcall]<nint, uint, PROPSHEETPAGEW*, uint> pfnCallback;
    public uint* pcRefParent;
    public char* pszHeaderTitle;
    public char* pszHeaderSubTitle;
    public nint hActCtx;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct DLGTEMPLATE
{
    public uint style;
    public uint dwExtendedStyle;
    public ushort cdit;
    public short x;
    public short y;
    public short cx;
    public short cy;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct LVCOLUMNW
{
    public uint mask;
    public int fmt;
    public int cx;
    public char* pszText;
    public int cchTextMax;
    public int iSubItem;
    public int iImage;
    public int iOrder;
    public int cxMin;
    public int cxDefault;
    public int cxIdeal;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct LVITEMW
{
    public uint mask;
    public int iItem;
    public int iSubItem;
    public uint state;
    public uint stateMask;
    public char* pszText;
    public int cchTextMax;
    public int iImage;
    public nint lParam;
    public int iIndent;
    public int iGroupId;
    public uint cColumns;
    public uint* puColumns;
    public int* piColFmt;
    public int iGroup;
}

[StructLayout(LayoutKind.Sequential)]
public struct NMHDR
{
    public nint hwndFrom;
    public nuint idFrom;
    public uint code;
}

[StructLayout(LayoutKind.Sequential)]
public struct NMLISTVIEW
{
    public NMHDR hdr;
    public int iItem;
    public int iSubItem;
    public uint uNewState;
    public uint uOldState;
    public uint uChanged;
    public POINT ptAction;
    public nint lParam;
}
