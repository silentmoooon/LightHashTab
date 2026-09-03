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

    // Static control styles
    public const uint SS_ENDELLIPSIS = 0x00004000;

    // GetWindowLongPtr / SetWindowLongPtr indices
    public const int GWL_STYLE = -16;

    // Window Messages
    public const uint WM_NULL = 0x0000;
    public const uint WM_CREATE = 0x0001;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_SIZE = 0x0005;
    public const uint WM_SETTEXT = 0x000C;
    public const uint WM_GETTEXT = 0x000D;
    public const uint WM_GETTEXTLENGTH = 0x000E;
    public const uint WM_CLOSE = 0x0010;
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

    // Edit control messages
    public const uint EM_SETCUEBANNER = WM_USER + 158; // EM_SETCUEBANNER = 0x1501

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

    public const uint PSPCB_ADDREF = 0;
    public const uint PSPCB_RELEASE = 1;
    public const uint PSPCB_CREATE = 2;

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
    public const uint LVM_GETCOLUMNWIDTH = LVM_FIRST + 29;
    public const uint LVM_SETCOLUMNWIDTH = LVM_FIRST + 30;

    public const uint LVN_FIRST = unchecked((uint)-100);
    public const uint LVN_GETINFOTIPW = unchecked(LVN_FIRST - 57);

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
    public const uint BS_AUTOCHECKBOX = 0x00000003;
    public const uint BM_GETCHECK = 0x00F0;
    public const uint BM_SETCHECK = 0x00F1;
    public const int BST_UNCHECKED = 0;
    public const int BST_CHECKED = 1;

    public const uint WS_CAPTION = 0x00C00000;
    public const uint WS_SYSMENU = 0x00080000;
    public const uint WS_POPUP = 0x80000000;
    public const uint WS_EX_DLGMODALFRAME = 0x00000001;

    public const uint DS_SETFONT = 0x00000040;
    public const uint DS_MODALFRAME = 0x00000080;
    public const uint DS_CENTER = 0x00000800;

    // Progress Bar Messages
    public const uint PBM_SETRANGE32 = WM_USER + 6;
    public const uint PBM_SETPOS = WM_USER + 2;
    public const uint PBM_SETSTATE = WM_USER + 16;
    public const uint PBM_SETMARQUEE = WM_USER + 10;
    public const uint PBS_SMOOTH = 0x01;
    public const uint PBS_MARQUEE = 0x08;

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

    public const uint SWP_NOMOVE = 0x0002;
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

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint GetClipboardData(uint uFormat);

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

    // x64: DWLP_MSGRESULT=0, DWLP_DLGPROC=sizeof(LRESULT)=8, DWLP_USER=sizeof(LRESULT)+sizeof(DLGPROC)=16
    // x86: DWLP_MSGRESULT=0, DWLP_DLGPROC=4,                DWLP_USER=8
    // We target win-x64 only, so DWLP_USER = 16.
    public const int DWLP_MSGRESULT = 0;
    public const int DWLP_DLGPROC   = 8;
    public const int DWLP_USER      = 16;
    public const int GWLP_USERDATA  = -21;

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern nint GetStockObject(int i);
    public const int DEFAULT_GUI_FONT = 17;
    public const int NULL_BRUSH = 5;

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InvalidateRect(nint hWnd, RECT* lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

    public static bool Invalidate(nint hWnd) => InvalidateRect(hWnd, null, true);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(nint hObject);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern nint CreateFontIndirectW(LOGFONTW* lplf);

    public const uint SPI_GETNONCLIENTMETRICS = 0x0029;

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SystemParametersInfoW(uint uiAction, uint uiParam, void* pvParam, uint fWinIni);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern uint SetTextColor(nint hdc, uint color);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern uint SetBkColor(nint hdc, uint color);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern int SetBkMode(nint hdc, int mode);
    public const int TRANSPARENT = 1;

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(nint hWnd, int nCmdShow);
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern nint SelectObject(nint hdc, nint hgdiobj);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    public static extern nint CreateSolidBrush(uint color);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern int FillRect(nint hDC, RECT* lprc, nint hbr);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(POINT* lpPoint);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ScreenToClient(nint hWnd, POINT* lpPoint);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint CreatePopupMenu();

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AppendMenuW(nint hMenu, uint uFlags, nuint uIDNewItem, [MarshalAs(UnmanagedType.LPWStr)] string? lpNewItem);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern uint TrackPopupMenuEx(nint hmenu, uint fuFlags, int x, int y, nint hwnd, void* lptpm);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(nint hIcon);

    [DllImport("shell32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern nuint SHGetFileInfoW(char* pszPath, uint dwFileAttributes, SHFILEINFOW* psfi, uint cbFileInfo, uint uFlags);

    [DllImport("shell32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern nint ShellExecuteW(nint hwnd, [MarshalAs(UnmanagedType.LPWStr)] string? lpOperation, [MarshalAs(UnmanagedType.LPWStr)] string lpFile, [MarshalAs(UnmanagedType.LPWStr)] string? lpParameters, [MarshalAs(UnmanagedType.LPWStr)] string? lpDirectory, int nShowCmd);

    // Menu constants
    public const uint MF_STRING = 0x0000;
    public const uint MF_SEPARATOR = 0x0800;
    public const uint MF_CHECKED = 0x0008;
    public const uint MF_UNCHECKED = 0x0000;

    public const uint TPM_LEFTALIGN = 0x0000;
    public const uint TPM_TOPALIGN = 0x0000;
    public const uint TPM_RETURNCMD = 0x0100;
    public const uint TPM_RIGHTBUTTON = 0x0002;

    // Shell file info constants
    public const uint SHGFI_ICON = 0x000000100;
    public const uint SHGFI_SMALLICON = 0x000000001;
    public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

    public const uint SS_ICON = 0x00000003;
    public const uint STM_SETICON = 0x0170;

    public const uint WM_CONTEXTMENU = 0x007B;

    public const uint NM_FIRST = 0U;
    public const uint NM_DBLCLK = unchecked(NM_FIRST - 3);
    public const uint NM_RCLICK = unchecked(NM_FIRST - 5);
    public const uint NM_CUSTOMDRAW = unchecked(NM_FIRST - 12);

    public const uint CDDS_PREPAINT = 0x00000001;
    public const uint CDDS_POSTPAINT = 0x00000002;
    public const uint CDDS_ITEM = 0x00010000;
    public const uint CDDS_ITEMPREPAINT = CDDS_ITEM | CDDS_PREPAINT;
    public const uint CDDS_SUBITEM = 0x00020000;

    public const nint CDRF_DODEFAULT = 0x00000000;
    public const nint CDRF_NEWFONT = 0x00000002;
    public const nint CDRF_NOTIFYITEMDRAW = 0x00000020;
    public const nint CDRF_NOTIFYSUBITEMDRAW = 0x00000020;

    public const uint LVS_EX_INFOTIP = 0x00000400;
    public const uint LVS_EX_LABELTIP = 0x00004000;
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint SetActiveWindow(nint hWnd);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern int GetMessageW(MSG* lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TranslateMessage(MSG* lpMsg);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint DispatchMessageW(MSG* lpMsg);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsDialogMessageW(nint hDlg, MSG* lpMsg);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint DefWindowProcW(nint hWnd, uint uMsg, nuint wParam, nint lParam);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint DialogBoxIndirectParamW(
        nint hInstance,
        DLGTEMPLATE* hDialogTemplate,
        nint hWndParent,
        delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint> lpDialogFunc,
        nint dwInitParam);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EndDialog(nint hDlg, nint nResult);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AdjustWindowRectEx(RECT* lpRect, uint dwStyle, [MarshalAs(UnmanagedType.Bool)] bool bMenu, uint dwExStyle);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint GetDesktopWindow();

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern uint GetDpiForWindow(nint hWnd);

    public static uint GetWindowDpi(nint hWnd)
    {
        try
        {
            if (hWnd != 0)
            {
                uint dpi = GetDpiForWindow(hWnd);
                if (dpi > 0) return dpi;
            }
        }
        catch { }
        return 96;
    }

    public static float GetDpiScale(nint hWnd)
    {
        return GetWindowDpi(hWnd) / 96.0f;
    }

    public static int Scale(int value, float scale) => (int)Math.Round(value * scale, MidpointRounding.AwayFromZero);

    public static void CenterWindow(nint hWnd, nint hWndCenter)
    {
        if (hWnd == 0) return;
        if (hWndCenter == 0) hWndCenter = GetDesktopWindow();

        if (GetWindowRect(hWnd, out RECT rc) && GetWindowRect(hWndCenter, out RECT rcCenter))
        {
            int width = rc.Width;
            int height = rc.Height;
            int x = rcCenter.Left + (rcCenter.Width - width) / 2;
            int y = rcCenter.Top + (rcCenter.Height - height) / 2;
            SetWindowPos(hWnd, 0, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern nint GetDC(nint hWnd);

    [DllImport("user32.dll", ExactSpelling = true)]
    public static extern int ReleaseDC(nint hWnd, nint hDC);

    [DllImport("gdi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetTextExtentPoint32W(nint hdc, char* lpString, int c, SIZE* lpSize);
}

[StructLayout(LayoutKind.Sequential)]
public struct SIZE
{
    public int cx;
    public int cy;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct NMLVGETINFOTIPW
{
    public NMHDR hdr;
    public uint dwFlags;
    public char* pszText;
    public int cchTextMax;
    public int iItem;
    public int iSubItem;
    public nint lParam;
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
public struct MSG
{
    public nint hwnd;
    public uint message;
    public nuint wParam;
    public nint lParam;
    public uint time;
    public POINT pt;
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
    public nint hbmHeader;
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

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct LOGFONTW
{
    public int lfHeight;
    public int lfWidth;
    public int lfEscapement;
    public int lfOrientation;
    public int lfWeight;
    public byte lfItalic;
    public byte lfUnderline;
    public byte lfStrikeOut;
    public byte lfCharSet;
    public byte lfOutPrecision;
    public byte lfClipPrecision;
    public byte lfQuality;
    public byte lfPitchAndFamily;
    public fixed char lfFaceName[32];
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct NONCLIENTMETRICSW
{
    public uint cbSize;
    public int iBorderWidth;
    public int iScrollWidth;
    public int iScrollHeight;
    public int iCaptionWidth;
    public int iCaptionHeight;
    public LOGFONTW lfCaptionFont;
    public int iSmCaptionWidth;
    public int iSmCaptionHeight;
    public LOGFONTW lfSmCaptionFont;
    public int iMenuWidth;
    public int iMenuHeight;
    public LOGFONTW lfMenuFont;
    public LOGFONTW lfStatusFont;
    public LOGFONTW lfMessageFont;
    public int iPaddedBorderWidth;
}

[StructLayout(LayoutKind.Sequential)]
public struct NMCUSTOMDRAW
{
    public NMHDR hdr;
    public uint dwDrawStage;
    public nint hdc;
    public RECT rc;
    public nuint dwItemSpec;
    public uint uItemState;
    public nint lItemlParam;
}

[StructLayout(LayoutKind.Sequential)]
public struct NMLVCUSTOMDRAW
{
    public NMCUSTOMDRAW nmcd;
    public uint clrText;
    public uint clrTextBk;
    public int iSubItem;
    public uint dwItemType;
    public uint clrFace;
    public int iIconEffect;
    public int iIconPhase;
    public int iPartId;
    public int iStateId;
    public RECT rcText;
    public uint uAlign;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct SHFILEINFOW
{
    public nint hIcon;
    public int iIcon;
    public uint dwAttributes;
    public fixed char szDisplayName[260];
    public fixed char szTypeName[80];
}

