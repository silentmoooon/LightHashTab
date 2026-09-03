using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightHashTab.Hashing;
using LightHashTab.Interop;

namespace LightHashTab.UI;

public static class PropertySheetPage
{
    private const int IDC_FILE_ICON       = 2000;
    private const int IDC_FILE_INFO       = 2001;
    private const int IDC_LIST_HASHES     = 2002;
    private const int IDC_PROGRESS        = 2003;
    private const int IDC_LABEL_COMPARE   = 2004;
    private const int IDC_EDIT_COMPARE    = 2005;
    private const int IDC_BTN_PASTE_CLEAR = 2006;
    private const int IDC_LABEL_MATCH     = 2007;
    private const int IDC_BTN_SETTINGS    = 2008;
    private const int IDC_BTN_COPY_SEL    = 2009;
    private const int IDC_BTN_COPY_ALL    = 2010;

    // Column indices
    private const int COL_ALGO   = 0;
    private const int COL_HASH   = 1;

    public sealed class PageState
    {
        public required List<string> FilePaths { get; init; }
        public required FileHashSummary Summary  { get; init; }
        public CancellationTokenSource? Cts      { get; set; }
        public GCHandle GcHandle                 { get; set; }

        public nint PTemplate  { get; set; }
        public nint PTitle     { get; set; }
        public nint HFont      { get; set; }   // System Segoe UI font (owned)
        public nint HFontMono  { get; set; }   // Monospace font (owned)
        public nint HIcon      { get; set; }   // Shell file icon (owned)

        public nint HwndDlg           { get; set; }
        public nint HwndFileIcon      { get; set; }
        public nint HwndFileInfo      { get; set; }
        public nint HwndListView      { get; set; }
        public nint HwndProgress      { get; set; }
        public nint HwndLabelCompare  { get; set; }
        public nint HwndEditCompare   { get; set; }
        public nint HwndBtnPasteClear { get; set; }
        public nint HwndLabelMatch    { get; set; }
        public nint HwndBtnSettings   { get; set; }
        public nint HwndBtnCopySel    { get; set; }
        public nint HwndBtnCopyAll    { get; set; }

        public bool IsComputing { get; set; } = true;
        public int  MatchedRow  { get; set; } = -1;
        public bool HasMismatch { get; set; }

        public void StartCalculation()
        {
            IsComputing = true;
            MatchedRow = -1;
            HasMismatch = false;
            Summary.ElapsedMs = 0;

            if (HwndFileInfo != 0 && HwndDlg != 0)
            {
                Win32.GetClientRect(HwndDlg, out RECT rc);
                float scale = Win32.GetDpiScale(HwndDlg);
                int pad = Win32.Scale(10, scale);
                int iconSize = Win32.Scale(16, scale);
                int infoW = (rc.Width - pad * 2) - (iconSize + Win32.Scale(6, scale));
                UpdateFileInfoLabel(this, infoW);
            }

            Cts?.Cancel();
            Cts = new CancellationTokenSource();
            var token = Cts.Token;
            var hwnd = HwndDlg;
            var filePath = Summary.FilePath;
            var hashList = Summary.Hashes;

            Task.Run(async () =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    await HashEngine.ComputeHashesAsync(
                        filePath, hashList,
                        onProgress: percent =>
                        {
                            if (hwnd != 0)
                                Win32.PostMessageW(hwnd, Win32.WM_APP_PROGRESS, (nuint)percent, 0);
                        },
                        token).ConfigureAwait(false);

                    sw.Stop();
                    Summary.ElapsedMs = sw.Elapsed.TotalMilliseconds;

                    if (hwnd != 0)
                        Win32.PostMessageW(hwnd, Win32.WM_APP_HASH_FINISHED, 0, 0);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    sw.Stop();
                    foreach (var h in hashList)
                        h.Status = $"Error: {ex.Message}";
                    if (hwnd != 0)
                        Win32.PostMessageW(hwnd, Win32.WM_APP_HASH_FINISHED, 0, 0);
                }
            }, token);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Font helper
    // ──────────────────────────────────────────────────────────────────────────
    private static unsafe nint CreateSystemFont()
    {
        NONCLIENTMETRICSW ncm = default;
        ncm.cbSize = (uint)sizeof(NONCLIENTMETRICSW);
        if (Win32.SystemParametersInfoW(Win32.SPI_GETNONCLIENTMETRICS, ncm.cbSize, &ncm, 0))
        {
            return Win32.CreateFontIndirectW(&ncm.lfMessageFont);
        }
        return Win32.GetStockObject(Win32.DEFAULT_GUI_FONT);
    }

    private static unsafe nint CreateMonospaceFont(nint hwnd)
    {
        float scale = Win32.GetDpiScale(hwnd);
        int fontHeight = -Win32.Scale(12, scale);

        NONCLIENTMETRICSW ncm = default;
        ncm.cbSize = (uint)sizeof(NONCLIENTMETRICSW);
        if (Win32.SystemParametersInfoW(Win32.SPI_GETNONCLIENTMETRICS, ncm.cbSize, &ncm, 0))
        {
            fontHeight = ncm.lfMessageFont.lfHeight;
        }

        string[] candidateFaces = ["Cascadia Mono", "Consolas", "Courier New"];
        foreach (var face in candidateFaces)
        {
            LOGFONTW lf = default;
            lf.lfHeight = fontHeight;
            lf.lfWeight = 400; // FW_NORMAL
            lf.lfPitchAndFamily = 1 | (3 << 4); // FIXED_PITCH | FF_MODERN
            lf.lfQuality = 5; // CLEARTYPE_QUALITY
            char* pDst = lf.lfFaceName;
            fixed (char* pSrc = face)
            {
                int len = Math.Min(face.Length, 31);
                Buffer.MemoryCopy(pSrc, pDst, 32 * sizeof(char), len * sizeof(char));
                pDst[len] = '\0';
            }
            nint hFont = Win32.CreateFontIndirectW(&lf);
            if (hFont != 0) return hFont;
        }

        return Win32.GetStockObject(Win32.DEFAULT_GUI_FONT);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CreatePage
    // ──────────────────────────────────────────────────────────────────────────
    public static unsafe nint CreatePage(List<string> filePaths)
    {
        if (filePaths.Count == 0) return 0;

        string firstFile = filePaths[0];
        long fileSize = 0;
        try { if (File.Exists(firstFile)) fileSize = new FileInfo(firstFile).Length; }
        catch { }

        var summary = new FileHashSummary
        {
            FilePath = firstFile,
            FileName = Path.GetFileName(firstFile),
            FileSize = fileSize,
            Hashes = AlgorithmConfig.GetActiveHashList()
        };

        var state = new PageState { FilePaths = filePaths, Summary = summary };
        var gcHandle = GCHandle.Alloc(state);
        state.GcHandle = gcHandle;

        // In-memory DLGTEMPLATE
        int templateSize = sizeof(DLGTEMPLATE) + 3 * sizeof(ushort);
        byte* pTemplate = (byte*)NativeMemory.AllocZeroed((nuint)templateSize);
        DLGTEMPLATE* dlg = (DLGTEMPLATE*)pTemplate;
        dlg->style = Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.DS_CONTROL;
        dlg->dwExtendedStyle = 0x00010000; // WS_EX_CONTROLPARENT
        dlg->cx = 240;
        dlg->cy = 200;
        state.PTemplate = (nint)pTemplate;

        nint pTitle = Marshal.StringToHGlobalUni("文件哈希");
        state.PTitle = pTitle;

        PROPSHEETPAGEW psp = new()
        {
            dwSize     = (uint)sizeof(PROPSHEETPAGEW),
            dwFlags    = Win32.PSP_DLGINDIRECT | Win32.PSP_USETITLE | Win32.PSP_USECALLBACK,
            pszTemplate = (char*)pTemplate,
            pszTitle   = (char*)pTitle,
            pfnDlgProc = &DialogProc,
            lParam     = GCHandle.ToIntPtr(gcHandle),
            pfnCallback = &PropSheetPageCallback,
        };

        nint hPage = Win32.CreatePropertySheetPageW(&psp);
        if (hPage == 0)
        {
            NativeMemory.Free(pTemplate);
            Marshal.FreeHGlobal(pTitle);
            gcHandle.Free();
        }
        return hPage;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe uint PropSheetPageCallback(nint hwnd, uint uMsg, PROPSHEETPAGEW* ppsp)
    {
        if (uMsg == Win32.PSPCB_RELEASE && ppsp != null && ppsp->lParam != 0)
        {
            var gcHandle = GCHandle.FromIntPtr(ppsp->lParam);
            if (gcHandle.IsAllocated && gcHandle.Target is PageState state)
            {
                state.Cts?.Cancel();
                if (state.HFont != 0) { Win32.DeleteObject(state.HFont); state.HFont = 0; }
                if (state.HFontMono != 0) { Win32.DeleteObject(state.HFontMono); state.HFontMono = 0; }
                if (state.HIcon != 0) { Win32.DestroyIcon(state.HIcon); state.HIcon = 0; }
                if (state.PTemplate != 0) { NativeMemory.Free((void*)state.PTemplate); state.PTemplate = 0; }
                if (state.PTitle != 0) { Marshal.FreeHGlobal(state.PTitle); state.PTitle = 0; }
                gcHandle.Free();
            }
        }
        return 1;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DialogProc
    // ──────────────────────────────────────────────────────────────────────────
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe nint DialogProc(nint hwndDlg, uint uMsg, nuint wParam, nint lParam)
    {
        switch (uMsg)
        {
            case Win32.WM_INITDIALOG:
            {
                PROPSHEETPAGEW* psp = (PROPSHEETPAGEW*)lParam;
                if (psp == null || psp->lParam == 0) return 1;
                var gcHandle = GCHandle.FromIntPtr(psp->lParam);
                if (!gcHandle.IsAllocated || gcHandle.Target is not PageState state) return 1;

                state.HwndDlg = hwndDlg;
                Win32.SetWindowLongPtrW(hwndDlg, Win32.DWLP_USER, GCHandle.ToIntPtr(gcHandle));

                // Common controls
                INITCOMMONCONTROLSEX icc = new()
                {
                    dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
                    dwICC = Win32.ICC_LISTVIEW_CLASSES | Win32.ICC_PROGRESS_CLASS | Win32.ICC_STANDARD_CLASSES
                };
                Win32.InitCommonControlsEx(&icc);

                state.HFont = CreateSystemFont();
                state.HFontMono = CreateMonospaceFont(hwndDlg);
                InitializeDialogControls(state);
                state.StartCalculation();
                return 1;
            }

            case Win32.WM_NOTIFY:
            {
                NMHDR* pnm = (NMHDR*)lParam;
                if (pnm != null)
                {
                    uint code = pnm->code;
                    if (code == Win32.PSN_SETACTIVE || code == Win32.PSN_KILLACTIVE || code == Win32.PSN_APPLY)
                    {
                        Win32.SetWindowLongPtrW(hwndDlg, Win32.DWLP_MSGRESULT, 0);
                        return 1;
                    }

                    if (pnm->idFrom == unchecked((nuint)IDC_LIST_HASHES))
                    {
                        if (code == Win32.NM_CUSTOMDRAW)
                        {
                            var state = GetState(hwndDlg);
                            if (state != null)
                            {
                                nint res = HandleListViewCustomDraw(state, (NMLVCUSTOMDRAW*)lParam);
                                Win32.SetWindowLongPtrW(hwndDlg, Win32.DWLP_MSGRESULT, res);
                                return 1;
                            }
                        }
                        else if (code == Win32.NM_DBLCLK)
                        {
                            var state = GetState(hwndDlg);
                            if (state != null)
                            {
                                CopySelectedHash(state);
                                Win32.SetWindowLongPtrW(hwndDlg, Win32.DWLP_MSGRESULT, 0);
                                return 1;
                            }
                        }
                        else if (code == Win32.LVN_GETINFOTIPW)
                        {
                            var state = GetState(hwndDlg);
                            if (state != null)
                            {
                                NMLVGETINFOTIPW* pit = (NMLVGETINFOTIPW*)lParam;
                                if (pit != null && pit->pszText != null && pit->cchTextMax > 0 &&
                                    pit->iItem >= 0 && pit->iItem < state.Summary.Hashes.Count)
                                {
                                    var item = state.Summary.Hashes[pit->iItem];
                                    string tip = $"{item.Name}:\n{item.Value}";
                                    fixed (char* pTip = tip)
                                    {
                                        int copyLen = Math.Min(tip.Length, pit->cchTextMax - 1);
                                        Buffer.MemoryCopy(pTip, pit->pszText, pit->cchTextMax * sizeof(char), copyLen * sizeof(char));
                                        pit->pszText[copyLen] = '\0';
                                    }
                                    return 1;
                                }
                            }
                        }
                    }
                }
                return 0;
            }

            case Win32.WM_SIZE:
            {
                var state = GetState(hwndDlg);
                if (state != null) LayoutControls(state);
                return 0;
            }

            case Win32.WM_COMMAND:
            {
                var state = GetState(hwndDlg);
                if (state == null) return 0;
                uint id   = (uint)(wParam & 0xFFFF);
                uint code = (uint)((wParam >> 16) & 0xFFFF);
                if (id == IDC_EDIT_COMPARE && code == 0x0300 /* EN_CHANGE */)
                    CheckHashMatch(state);
                else if (id == IDC_BTN_PASTE_CLEAR)
                    HandlePasteOrClear(state);
                else if (id == IDC_BTN_SETTINGS)
                    ShowSettings(state);
                else if (id == IDC_BTN_COPY_SEL)
                    CopySelectedHash(state);
                else if (id == IDC_BTN_COPY_ALL)
                    CopyAllHashes(state);
                return 0;
            }

            case Win32.WM_CONTEXTMENU:
            {
                var state = GetState(hwndDlg);
                if (state != null && (nint)wParam == state.HwndListView)
                {
                    int x = (short)(lParam & 0xFFFF);
                    int y = (short)((lParam >> 16) & 0xFFFF);
                    ShowListViewContextMenu(state, x, y);
                    return 1;
                }
                return 0;
            }

            case Win32.WM_CTLCOLORSTATIC:
            {
                var state = GetState(hwndDlg);
                if (state != null && (nint)lParam == state.HwndLabelMatch)
                {
                    nint hdc = (nint)wParam;
                    Win32.SetBkMode(hdc, Win32.TRANSPARENT);
                    bool isDark = ThemeHelper.IsDarkMode();
                    if (state.MatchedRow >= 0)
                    {
                        Win32.SetTextColor(hdc, (uint)(isDark ? 0x0066EE66 : 0x000A820A));
                    }
                    else if (state.HasMismatch)
                    {
                        Win32.SetTextColor(hdc, (uint)(isDark ? 0x006666FF : 0x000000D0));
                    }
                    return Win32.GetStockObject(Win32.NULL_BRUSH);
                }
                return 0;
            }

            case Win32.WM_APP_PROGRESS:
            {
                var state = GetState(hwndDlg);
                if (state != null && state.HwndProgress != 0)
                    Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETPOS, wParam, 0);
                return 0;
            }

            case Win32.WM_APP_HASH_FINISHED:
            {
                var state = GetState(hwndDlg);
                if (state == null) return 0;

                state.IsComputing = false;

                // Hide progress bar once finished
                if (state.HwndProgress != 0)
                {
                    Win32.ShowWindow(state.HwndProgress, Win32.SW_HIDE);
                }

                // Update File Info with computation time and throughput
                Win32.GetClientRect(state.HwndDlg, out RECT rc);
                float scale = Win32.GetDpiScale(state.HwndDlg);
                int pad = Win32.Scale(10, scale);
                int iconSize = Win32.Scale(16, scale);
                int infoW = (rc.Width - pad * 2) - (iconSize + Win32.Scale(6, scale));
                UpdateFileInfoLabel(state, infoW);

                UpdateListViewItems(state);
                LayoutControls(state);
                CheckHashMatch(state);
                return 0;
            }

            case Win32.WM_DESTROY:
            {
                var state = GetState(hwndDlg);
                if (state != null)
                {
                    state.Cts?.Cancel();
                    if (state.HIcon != 0) { Win32.DestroyIcon(state.HIcon); state.HIcon = 0; }
                    state.HwndDlg = 0;
                    Win32.SetWindowLongPtrW(hwndDlg, Win32.DWLP_USER, 0);
                }
                return 0;
            }
        }
        return 0;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────
    private static PageState? GetState(nint hwndDlg)
    {
        if (hwndDlg == 0) return null;
        nint ptr = Win32.GetWindowLongPtrW(hwndDlg, Win32.DWLP_USER);
        if (ptr == 0) return null;
        var gcHandle = GCHandle.FromIntPtr(ptr);
        return gcHandle.IsAllocated ? gcHandle.Target as PageState : null;
    }

    private static unsafe void InitializeDialogControls(PageState state)
    {
        nint hwnd  = state.HwndDlg;
        nint hFont = state.HFont;

        // ── File Icon ─────────────────────────────────────────────────────────
        SHFILEINFOW sfi = default;
        fixed (char* pPath = state.Summary.FilePath)
        {
            Win32.SHGetFileInfoW(pPath, 0, &sfi, (uint)sizeof(SHFILEINFOW), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON);
        }
        if (sfi.hIcon != 0)
        {
            state.HIcon = sfi.hIcon;
            state.HwndFileIcon = Win32.CreateWindowExW(
                0, "STATIC", "",
                Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.SS_ICON,
                0, 0, 16, 16, hwnd, (nint)IDC_FILE_ICON, 0, null);
            if (state.HwndFileIcon != 0)
                Win32.SendMessageW(state.HwndFileIcon, Win32.STM_SETICON, (nuint)state.HIcon, 0);
        }

        // ── File info label ───────────────────────────────────────────────────
        state.HwndFileInfo = Win32.CreateWindowExW(
            0, "STATIC", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE,
            0, 0, 300, 20, hwnd, (nint)IDC_FILE_INFO, 0, null);

        // ── List View ─────────────────────────────────────────────────────────
        state.HwndListView = Win32.CreateWindowExW(
            0, "SysListView32", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_BORDER
            | Win32.LVS_REPORT | Win32.LVS_SINGLESEL | Win32.LVS_SHOWSELALWAYS | Win32.LVS_NOSORTHEADER,
            0, 0, 300, 140, hwnd, (nint)IDC_LIST_HASHES, 0, null);

        if (state.HwndListView != 0)
        {
            Win32.SendMessageW(state.HwndListView, Win32.LVM_SETEXTENDEDLISTVIEWSTYLE, 0,
                (nint)(Win32.LVS_EX_FULLROWSELECT | Win32.LVS_EX_DOUBLEBUFFER | Win32.LVS_EX_INFOTIP | Win32.LVS_EX_LABELTIP));
            ThemeHelper.ApplyTheme(state.HwndListView);

            float scale = Win32.GetDpiScale(hwnd);
            int algoColW = Win32.Scale(110, scale);
            AddColumn(state.HwndListView, COL_ALGO, "算法", algoColW);
            AddColumn(state.HwndListView, COL_HASH, "哈希值", 300);

            for (int i = 0; i < state.Summary.Hashes.Count; i++)
                InsertRow(state.HwndListView, i, state.Summary.Hashes[i].Name, "计算中…");
        }

        // ── Progress bar (starts in Marquee mode) ─────────────────────────────
        state.HwndProgress = Win32.CreateWindowExW(
            0, "msctls_progress32", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.PBS_MARQUEE,
            0, 0, 300, 5, hwnd, (nint)IDC_PROGRESS, 0, null);

        if (state.HwndProgress != 0)
        {
            ThemeHelper.ApplyTheme(state.HwndProgress);
            Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETMARQUEE, 1, 40);
        }

        // ── Compare label ────────────────────────────────────────────────────
        state.HwndLabelCompare = Win32.CreateWindowExW(
            0, "STATIC", "比对:",
            Win32.WS_CHILD | Win32.WS_VISIBLE,
            0, 0, 48, 20, hwnd, (nint)IDC_LABEL_COMPARE, 0, null);

        // ── Compare edit box ──────────────────────────────────────────────────
        state.HwndEditCompare = Win32.CreateWindowExW(
            0, "EDIT", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_BORDER
            | Win32.ES_AUTOHSCROLL | Win32.WS_TABSTOP,
            0, 0, 200, 24, hwnd, (nint)IDC_EDIT_COMPARE, 0, null);

        if (state.HwndEditCompare != 0)
        {
            fixed (char* pCue = "在此粘贴哈希值以进行比对…")
                Win32.SendMessageW(state.HwndEditCompare, Win32.EM_SETCUEBANNER, 1, (nint)pCue);
        }

        // ── Paste / Clear button ──────────────────────────────────────────────
        state.HwndBtnPasteClear = Win32.CreateWindowExW(
            0, "BUTTON", "粘贴",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            0, 0, 56, 24, hwnd, (nint)IDC_BTN_PASTE_CLEAR, 0, null);

        // ── Match result label (left-aligned) ─────────────────────────────────
        state.HwndLabelMatch = Win32.CreateWindowExW(
            0, "STATIC", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.SS_ENDELLIPSIS,
            0, 0, 150, 22, hwnd, (nint)IDC_LABEL_MATCH, 0, null);

        // ── Settings button ───────────────────────────────────────────────────
        state.HwndBtnSettings = Win32.CreateWindowExW(
            0, "BUTTON", "⚙ 设置",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            0, 0, 76, 24, hwnd, (nint)IDC_BTN_SETTINGS, 0, null);

        // ── Copy buttons ──────────────────────────────────────────────────────
        state.HwndBtnCopySel = Win32.CreateWindowExW(
            0, "BUTTON", "复制",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            0, 0, 56, 24, hwnd, (nint)IDC_BTN_COPY_SEL, 0, null);

        state.HwndBtnCopyAll = Win32.CreateWindowExW(
            0, "BUTTON", "复制全部",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            0, 0, 78, 24, hwnd, (nint)IDC_BTN_COPY_ALL, 0, null);

        // ── Apply font to controls ───────────────────────────────────────────
        nint[] controls =
        [
            state.HwndFileInfo, state.HwndListView,
            state.HwndLabelCompare, state.HwndEditCompare, state.HwndBtnPasteClear,
            state.HwndLabelMatch, state.HwndBtnSettings, state.HwndBtnCopySel, state.HwndBtnCopyAll
        ];
        foreach (var c in controls)
            if (c != 0) Win32.SendMessageW(c, Win32.WM_SETFONT, (nuint)hFont, 1);

        LayoutControls(state);
    }

    private static unsafe void LayoutControls(PageState state)
    {
        if (state.HwndDlg == 0) return;
        Win32.GetClientRect(state.HwndDlg, out RECT rc);
        int w = rc.Width;
        int h = rc.Height;
        if (w < 10 || h < 10) return;

        float scale = Win32.GetDpiScale(state.HwndDlg);

        int pad      = Win32.Scale(10, scale);
        int gap      = Win32.Scale(6, scale);
        int editH    = Win32.Scale(25, scale);
        int btnH     = Win32.Scale(25, scale);
        int infoH    = Win32.Scale(18, scale);
        int iconSize = Win32.Scale(16, scale);

        bool showProg = state.IsComputing;
        int progH     = showProg ? Win32.Scale(4, scale) : 0;
        int progGap   = showProg ? gap : 0;

        int btnCopyW     = Win32.Scale(58, scale);   // "复制"
        int btnCopyAllW  = Win32.Scale(80, scale);   // "复制全部"
        int btnSettingsW = Win32.Scale(76, scale);   // "⚙ 设置"
        int btnPasteClrW = Win32.Scale(58, scale);   // "粘贴" / "清除"
        int lblCompareW  = Win32.Scale(46, scale);   // "比对:"

        int contentW = w - pad * 2;

        int btnRowBottom     = h - pad;
        int btnRowTop        = btnRowBottom - btnH;
        int compareRowBottom = btnRowTop - gap;
        int compareRowTop    = compareRowBottom - editH;
        int progRowBottom    = compareRowTop - progGap;
        int progRowTop       = progRowBottom - progH;

        int listTop    = pad + infoH + gap;
        int listBottom = showProg ? (progRowTop - gap) : (compareRowTop - gap);
        int listH      = Math.Max(Win32.Scale(40, scale), listBottom - listTop);

        // ── File Icon & Info label ─────────────────────────────────────────────
        int infoX = pad;
        int infoW = contentW;
        if (state.HwndFileIcon != 0)
        {
            Win32.SetWindowPos(state.HwndFileIcon, 0,
                pad, pad + 1, iconSize, iconSize,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
            infoX += iconSize + Win32.Scale(6, scale);
            infoW -= (iconSize + Win32.Scale(6, scale));
        }

        if (state.HwndFileInfo != 0)
        {
            Win32.SetWindowPos(state.HwndFileInfo, 0,
                infoX, pad, infoW, infoH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

            UpdateFileInfoLabel(state, infoW);
        }

        // ── List View ──────────────────────────────────────────────────────────
        if (state.HwndListView != 0)
        {
            Win32.SetWindowPos(state.HwndListView, 0,
                pad, listTop, contentW, listH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

            AdjustHashColumnWidth(state, contentW);
        }

        // ── Progress Bar ───────────────────────────────────────────────────────
        if (state.HwndProgress != 0)
        {
            if (showProg)
            {
                Win32.ShowWindow(state.HwndProgress, Win32.SW_SHOW);
                Win32.SetWindowPos(state.HwndProgress, 0,
                    pad, progRowTop, contentW, progH,
                    Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
            }
            else
            {
                Win32.ShowWindow(state.HwndProgress, Win32.SW_HIDE);
            }
        }

        // ── Compare Row: [比对:] [Edit Box] [粘贴/清除] ──────────────────
        if (state.HwndLabelCompare != 0)
            Win32.SetWindowPos(state.HwndLabelCompare, 0,
                pad, compareRowTop + Win32.Scale(3, scale), lblCompareW, editH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        int editX = pad + lblCompareW + gap;
        int editW = Math.Max(60, contentW - lblCompareW - gap - btnPasteClrW - gap);
        if (state.HwndEditCompare != 0)
            Win32.SetWindowPos(state.HwndEditCompare, 0,
                editX, compareRowTop, editW, editH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        int pasteClrX = editX + editW + gap;
        if (state.HwndBtnPasteClear != 0)
            Win32.SetWindowPos(state.HwndBtnPasteClear, 0,
                pasteClrX, compareRowTop, btnPasteClrW, editH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        // ── Button Row: [Match Label] [⚙ 设置] [复制] [复制全部] ───────────
        int rightEdge = pad + contentW;
        int copyAllX  = rightEdge - btnCopyAllW;
        int copySelX  = copyAllX - gap - btnCopyW;
        int settingsX = copySelX - gap - btnSettingsW;
        int matchLabelW = Math.Max(10, settingsX - pad - gap);

        if (state.HwndLabelMatch != 0)
            Win32.SetWindowPos(state.HwndLabelMatch, 0,
                pad, btnRowTop + Win32.Scale(3, scale), matchLabelW, btnH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        if (state.HwndBtnSettings != 0)
            Win32.SetWindowPos(state.HwndBtnSettings, 0,
                settingsX, btnRowTop, btnSettingsW, btnH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        if (state.HwndBtnCopySel != 0)
            Win32.SetWindowPos(state.HwndBtnCopySel, 0,
                copySelX, btnRowTop, btnCopyW, btnH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        if (state.HwndBtnCopyAll != 0)
            Win32.SetWindowPos(state.HwndBtnCopyAll, 0,
                copyAllX, btnRowTop, btnCopyAllW, btnH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
    }

    private static unsafe void UpdateFileInfoLabel(PageState state, int availableWidth)
    {
        if (state.HwndFileInfo == 0 || state.HwndDlg == 0) return;

        string sizeStr = FormatFileSize(state.Summary.FileSize);
        string speedText = state.IsComputing
            ? "  ·  计算中…"
            : FormatComputationSpeed(state.Summary.ElapsedMs, state.Summary.FileSize);

        string metaInfo = $"({sizeStr}{speedText})";
        string fileName = state.Summary.FileName;

        if (availableWidth <= 60)
        {
            Win32.SetWindowTextW(state.HwndFileInfo, $"{fileName}  {metaInfo}");
            return;
        }

        nint hdc = Win32.GetDC(state.HwndFileInfo);
        if (hdc != 0)
        {
            nint hOldFont = Win32.SelectObject(hdc, state.HFont != 0 ? state.HFont : Win32.GetStockObject(Win32.DEFAULT_GUI_FONT));

            SIZE metaSize;
            fixed (char* pMeta = metaInfo)
                Win32.GetTextExtentPoint32W(hdc, pMeta, metaInfo.Length, &metaSize);

            int spaceW = 12;
            int maxNameW = availableWidth - metaSize.cx - spaceW;

            string displayFileName = fileName;
            if (maxNameW > 20)
            {
                SIZE nameSize;
                fixed (char* pName = fileName)
                    Win32.GetTextExtentPoint32W(hdc, pName, fileName.Length, &nameSize);

                if (nameSize.cx > maxNameW)
                {
                    string ext = Path.GetExtension(fileName);
                    string stem = Path.GetFileNameWithoutExtension(fileName);

                    int low = 1, high = stem.Length;
                    string bestName = stem + ext;

                    while (low <= high)
                    {
                        int mid = (low + high) / 2;
                        string candidate = stem[..mid] + "…" + ext;
                        SIZE candSize;
                        fixed (char* pCand = candidate)
                            Win32.GetTextExtentPoint32W(hdc, pCand, candidate.Length, &candSize);

                        if (candSize.cx <= maxNameW)
                        {
                            bestName = candidate;
                            low = mid + 1;
                        }
                        else
                        {
                            high = mid - 1;
                        }
                    }
                    displayFileName = bestName;
                }
            }

            Win32.SelectObject(hdc, hOldFont);
            Win32.ReleaseDC(state.HwndFileInfo, hdc);

            string fullText = $"{displayFileName}  {metaInfo}";
            Win32.SetWindowTextW(state.HwndFileInfo, fullText);
        }
        else
        {
            Win32.SetWindowTextW(state.HwndFileInfo, $"{fileName}  {metaInfo}");
        }
    }

    private static unsafe void AdjustHashColumnWidth(PageState state, int contentW)
    {
        if (state.HwndListView == 0 || state.HwndDlg == 0) return;

        float scale = Win32.GetDpiScale(state.HwndDlg);
        int algoColW = Win32.Scale(110, scale);

        int maxLen = 0;
        foreach (var h in state.Summary.Hashes)
        {
            if (!string.IsNullOrEmpty(h.Value) && h.Value.Length > maxLen)
                maxLen = h.Value.Length;
        }

        int fillW = Math.Max(60, contentW - algoColW - 4);
        int neededW = fillW;

        if (maxLen > 0)
        {
            nint hdc = Win32.GetDC(state.HwndListView);
            if (hdc != 0)
            {
                nint fontToUse = state.HFontMono != 0 ? state.HFontMono : state.HFont;
                nint hOldFont = Win32.SelectObject(hdc, fontToUse);
                SIZE size;
                string sample = new string('0', maxLen);
                fixed (char* pSample = sample)
                {
                    Win32.GetTextExtentPoint32W(hdc, pSample, sample.Length, &size);
                }
                Win32.SelectObject(hdc, hOldFont);
                Win32.ReleaseDC(state.HwndListView, hdc);

                // Add cell padding (margins, borders)
                neededW = size.cx + Win32.Scale(32, scale);
            }
        }

        int finalHashW = Math.Max(fillW, neededW);
        Win32.SendMessageW(state.HwndListView, Win32.LVM_SETCOLUMNWIDTH, COL_ALGO, algoColW);
        Win32.SendMessageW(state.HwndListView, Win32.LVM_SETCOLUMNWIDTH, COL_HASH, finalHashW);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ListView helpers
    // ──────────────────────────────────────────────────────────────────────────
    private static unsafe void AddColumn(nint hwndList, int index, string text, int width)
    {
        if (hwndList == 0) return;
        fixed (char* pText = text)
        {
            LVCOLUMNW col = new()
            {
                mask     = Win32.LVCF_TEXT | Win32.LVCF_WIDTH | Win32.LVCF_FMT,
                fmt      = Win32.LVCFMT_LEFT,
                cx       = width,
                pszText  = pText
            };
            Win32.SendMessageW(hwndList, Win32.LVM_INSERTCOLUMNW, (nuint)index, (nint)(&col));
        }
    }

    private static unsafe void InsertRow(nint hwndList, int row, string col0, string col1)
    {
        if (hwndList == 0) return;
        fixed (char* p0 = col0)
        {
            LVITEMW item = new() { mask = Win32.LVIF_TEXT, iItem = row, iSubItem = 0, pszText = p0 };
            Win32.SendMessageW(hwndList, Win32.LVM_INSERTITEMW, 0, (nint)(&item));
        }
        SetSubItem(hwndList, row, COL_HASH, col1);
    }

    private static unsafe void SetSubItem(nint hwndList, int row, int sub, string text)
    {
        if (hwndList == 0) return;
        fixed (char* pText = text)
        {
            LVITEMW item = new() { mask = Win32.LVIF_TEXT, iItem = row, iSubItem = sub, pszText = pText };
            Win32.SendMessageW(hwndList, Win32.LVM_SETITEMW, 0, (nint)(&item));
        }
    }

    private static void UpdateListViewItems(PageState state)
    {
        if (state.HwndListView == 0) return;
        for (int i = 0; i < state.Summary.Hashes.Count; i++)
        {
            var h = state.Summary.Hashes[i];
            SetSubItem(state.HwndListView, i, COL_HASH, h.Value);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CustomDraw handler
    // ──────────────────────────────────────────────────────────────────────────
    private static unsafe nint HandleListViewCustomDraw(PageState state, NMLVCUSTOMDRAW* plvcd)
    {
        if (plvcd == null) return Win32.CDRF_DODEFAULT;

        switch (plvcd->nmcd.dwDrawStage)
        {
            case Win32.CDDS_PREPAINT:
                return Win32.CDRF_NOTIFYITEMDRAW;

            case Win32.CDDS_ITEMPREPAINT:
                return Win32.CDRF_NOTIFYSUBITEMDRAW;

            case (Win32.CDDS_SUBITEM | Win32.CDDS_ITEMPREPAINT):
            {
                int row = (int)plvcd->nmcd.dwItemSpec;
                int col = plvcd->iSubItem;

                bool isMatched = (row == state.MatchedRow);
                bool isDark = ThemeHelper.IsDarkMode();

                if (isMatched)
                {
                    plvcd->clrTextBk = (uint)(isDark ? 0x001B4B1B : 0x00E6F7E6);
                    plvcd->clrText   = (uint)(isDark ? 0x0080FF80 : 0x000E630E);
                }

                if (col == COL_HASH && state.HFontMono != 0)
                {
                    Win32.SelectObject(plvcd->nmcd.hdc, state.HFontMono);
                    return Win32.CDRF_NEWFONT;
                }

                return isMatched ? (Win32.CDRF_NEWFONT | Win32.CDRF_DODEFAULT) : Win32.CDRF_DODEFAULT;
            }

            default:
                return Win32.CDRF_DODEFAULT;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Compare / Match
    // ──────────────────────────────────────────────────────────────────────────
    private static unsafe void CheckHashMatch(PageState state)
    {
        if (state.HwndEditCompare == 0 || state.HwndLabelMatch == 0) return;

        int length = Win32.GetWindowTextLengthW(state.HwndEditCompare);
        if (state.HwndBtnPasteClear != 0)
        {
            Win32.SetWindowTextW(state.HwndBtnPasteClear, length == 0 ? "粘贴" : "清除");
        }

        if (length == 0)
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, "");
            state.MatchedRow = -1;
            state.HasMismatch = false;
            Win32.Invalidate(state.HwndListView);
            Win32.Invalidate(state.HwndLabelMatch);
            return;
        }

        char* pBuf = stackalloc char[length + 2];
        int read = Win32.GetWindowTextW(state.HwndEditCompare, pBuf, length + 1);
        string raw = read > 0 ? new string(pBuf, 0, read).Trim().Trim('"', '\'', '`') : string.Empty;

        // Strip optional 0x hex prefix
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            raw = raw[2..];

        if (string.IsNullOrEmpty(raw))
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, "");
            state.MatchedRow = -1;
            state.HasMismatch = false;
            Win32.Invalidate(state.HwndListView);
            Win32.Invalidate(state.HwndLabelMatch);
            return;
        }

        if (state.IsComputing)
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, "正在计算…");
            state.MatchedRow = -1;
            state.HasMismatch = false;
            Win32.Invalidate(state.HwndLabelMatch);
            return;
        }

        int matchRow = -1;
        string matchName = "";
        for (int i = 0; i < state.Summary.Hashes.Count; i++)
        {
            var h = state.Summary.Hashes[i];
            if (!string.IsNullOrEmpty(h.Value) &&
                string.Equals(h.Value, raw, StringComparison.OrdinalIgnoreCase))
            {
                matchRow = i;
                matchName = h.Name;
                break;
            }
        }

        state.MatchedRow = matchRow;
        state.HasMismatch = matchRow < 0 && raw.Length >= 8;

        if (matchRow >= 0)
            Win32.SetWindowTextW(state.HwndLabelMatch, $"✔ 匹配成功: {matchName}");
        else if (state.HasMismatch)
            Win32.SetWindowTextW(state.HwndLabelMatch, "✖ 未匹配");
        else
            Win32.SetWindowTextW(state.HwndLabelMatch, "");

        Win32.Invalidate(state.HwndListView);
        Win32.Invalidate(state.HwndLabelMatch);
    }

    private static void HandlePasteOrClear(PageState state)
    {
        if (state.HwndEditCompare == 0) return;
        int len = Win32.GetWindowTextLengthW(state.HwndEditCompare);
        if (len > 0)
        {
            Win32.SetWindowTextW(state.HwndEditCompare, "");
        }
        else
        {
            string clip = GetClipboardText(state.HwndDlg);
            if (!string.IsNullOrEmpty(clip))
            {
                Win32.SetWindowTextW(state.HwndEditCompare, clip.Trim());
            }
        }
    }

    private static void ToggleCase(PageState state)
    {
        state.Summary.IsUppercase = !state.Summary.IsUppercase;
        foreach (var h in state.Summary.Hashes)
        {
            if (!string.IsNullOrEmpty(h.Value))
            {
                h.Value = state.Summary.IsUppercase ? h.Value.ToUpperInvariant() : h.Value.ToLowerInvariant();
            }
        }
        UpdateListViewItems(state);
        CheckHashMatch(state);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Context Menu
    // ──────────────────────────────────────────────────────────────────────────
    private static unsafe void ShowListViewContextMenu(PageState state, int x, int y)
    {
        if (state.HwndListView == 0) return;

        if (x == -1 && y == -1)
        {
            POINT pt;
            Win32.GetCursorPos(&pt);
            x = pt.X;
            y = pt.Y;
        }

        int sel = (int)Win32.SendMessageW(state.HwndListView, Win32.LVM_GETNEXTITEM, unchecked((nuint)(-1)), Win32.LVNI_SELECTED);
        bool hasSel = sel >= 0 && sel < state.Summary.Hashes.Count;
        string selAlgo = hasSel ? state.Summary.Hashes[sel].Name : "";
        string selHash = hasSel ? state.Summary.Hashes[sel].Value : "";

        nint hMenu = Win32.CreatePopupMenu();
        if (hMenu == 0) return;

        const uint IDM_COPY_HASH     = 3001;
        const uint IDM_COPY_LINE     = 3002;
        const uint IDM_COPY_ALL      = 3003;
        const uint IDM_TOGGLE_CASE   = 3004;
        const uint IDM_SEARCH_VT     = 3005;
        const uint IDM_SEARCH_GOOGLE = 3006;
        const uint IDM_SETTINGS      = 3007;

        uint selFlag = (hasSel && !string.IsNullOrEmpty(selHash)) ? Win32.MF_STRING : (Win32.MF_STRING | 0x00000001 /* MF_GRAYED */);

        Win32.AppendMenuW(hMenu, selFlag, IDM_COPY_HASH, hasSel ? $"复制 {selAlgo} 哈希值" : "复制哈希值");
        Win32.AppendMenuW(hMenu, selFlag, IDM_COPY_LINE, "复制算法与哈希");
        Win32.AppendMenuW(hMenu, Win32.MF_STRING, IDM_COPY_ALL, "复制全部哈希");
        Win32.AppendMenuW(hMenu, Win32.MF_SEPARATOR, 0, null);
        Win32.AppendMenuW(hMenu, Win32.MF_STRING | (state.Summary.IsUppercase ? Win32.MF_CHECKED : Win32.MF_UNCHECKED), IDM_TOGGLE_CASE, "大写格式 (ABCDEF...)");
        Win32.AppendMenuW(hMenu, Win32.MF_SEPARATOR, 0, null);
        Win32.AppendMenuW(hMenu, selFlag, IDM_SEARCH_VT, "在 VirusTotal 上搜索");
        Win32.AppendMenuW(hMenu, selFlag, IDM_SEARCH_GOOGLE, "在 Google 上搜索");
        Win32.AppendMenuW(hMenu, Win32.MF_SEPARATOR, 0, null);
        Win32.AppendMenuW(hMenu, Win32.MF_STRING, IDM_SETTINGS, "⚙ 算法设置...");

        uint cmd = Win32.TrackPopupMenuEx(hMenu, Win32.TPM_RETURNCMD | Win32.TPM_RIGHTBUTTON, x, y, state.HwndDlg, null);
        Win32.DestroyMenu(hMenu);

        switch (cmd)
        {
            case IDM_COPY_HASH:
                if (hasSel && !string.IsNullOrEmpty(selHash))
                {
                    CopyToClipboard(state.HwndDlg, selHash);
                    Win32.SetWindowTextW(state.HwndLabelMatch, $"✓ 已复制 {selAlgo} 哈希值！");
                    Win32.Invalidate(state.HwndLabelMatch);
                }
                break;
            case IDM_COPY_LINE:
                if (hasSel && !string.IsNullOrEmpty(selHash))
                {
                    CopyToClipboard(state.HwndDlg, $"{selAlgo}: {selHash}");
                    Win32.SetWindowTextW(state.HwndLabelMatch, $"✓ 已复制 {selAlgo} 整行！");
                    Win32.Invalidate(state.HwndLabelMatch);
                }
                break;
            case IDM_COPY_ALL:
                CopyAllHashes(state);
                break;
            case IDM_TOGGLE_CASE:
                ToggleCase(state);
                break;
            case IDM_SEARCH_VT:
                if (hasSel && !string.IsNullOrEmpty(selHash))
                    Win32.ShellExecuteW(state.HwndDlg, "open", $"https://www.virustotal.com/gui/search/{selHash}", null, null, 1);
                break;
            case IDM_SEARCH_GOOGLE:
                if (hasSel && !string.IsNullOrEmpty(selHash))
                    Win32.ShellExecuteW(state.HwndDlg, "open", $"https://www.google.com/search?q={selHash}", null, null, 1);
                break;
            case IDM_SETTINGS:
                ShowSettings(state);
                break;
        }
    }

    private static void ShowSettings(PageState state)
    {
        bool changed = AlgorithmSettingsDialog.Show(state.HwndDlg);
        if (changed)
        {
            state.Cts?.Cancel();
            state.Summary.Hashes.Clear();
            state.Summary.Hashes.AddRange(AlgorithmConfig.GetActiveHashList());
            if (state.Summary.IsUppercase)
            {
                foreach (var h in state.Summary.Hashes)
                    if (!string.IsNullOrEmpty(h.Value))
                        h.Value = h.Value.ToUpperInvariant();
            }

            if (state.HwndListView != 0)
            {
                Win32.SendMessageW(state.HwndListView, Win32.LVM_DELETEALLITEMS, 0, 0);
                for (int i = 0; i < state.Summary.Hashes.Count; i++)
                {
                    InsertRow(state.HwndListView, i, state.Summary.Hashes[i].Name, "计算中…");
                }
            }

            state.StartCalculation();
            LayoutControls(state);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Clipboard
    // ──────────────────────────────────────────────────────────────────────────
    private static void CopySelectedHash(PageState state)
    {
        if (state.HwndListView == 0) return;
        int sel = (int)Win32.SendMessageW(state.HwndListView, Win32.LVM_GETNEXTITEM,
                      unchecked((nuint)(-1)), Win32.LVNI_SELECTED);
        if (sel >= 0 && sel < state.Summary.Hashes.Count)
        {
            string v = state.Summary.Hashes[sel].Value;
            if (!string.IsNullOrEmpty(v))
            {
                CopyToClipboard(state.HwndDlg, v);
                Win32.SetWindowTextW(state.HwndLabelMatch, $"✓ 已复制 {state.Summary.Hashes[sel].Name} 哈希值！");
                Win32.Invalidate(state.HwndLabelMatch);
            }
        }
    }

    private static void CopyAllHashes(PageState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"文件 : {state.Summary.FileName}");
        sb.AppendLine($"大小 : {state.Summary.FileSize} 字节 ({FormatFileSize(state.Summary.FileSize)})");
        if (state.Summary.ElapsedMs > 0)
            sb.AppendLine($"耗时 : {Math.Round(state.Summary.ElapsedMs)} 毫秒");
        sb.AppendLine(new string('-', 64));
        foreach (var h in state.Summary.Hashes)
            sb.AppendLine($"{h.Name,-8}  {h.Value}");
        CopyToClipboard(state.HwndDlg, sb.ToString());
        Win32.SetWindowTextW(state.HwndLabelMatch, "✓ 已复制全部哈希值！");
        Win32.Invalidate(state.HwndLabelMatch);
    }

    private static unsafe void CopyToClipboard(nint hwndOwner, string text)
    {
        if (string.IsNullOrEmpty(text) || !Win32.OpenClipboard(hwndOwner)) return;
        try
        {
            Win32.EmptyClipboard();
            int bytes = (text.Length + 1) * sizeof(char);
            nint hMem = Win32.GlobalAlloc(Win32.GMEM_MOVEABLE, (nuint)bytes);
            if (hMem == 0) return;
            void* pMem = Win32.GlobalLock(hMem);
            if (pMem != null)
            {
                fixed (char* pText = text)
                    Buffer.MemoryCopy(pText, pMem, bytes, bytes);
                Win32.GlobalUnlock(hMem);
                Win32.SetClipboardData(Win32.CF_UNICODETEXT, hMem);
            }
        }
        finally { Win32.CloseClipboard(); }
    }

    private static unsafe string GetClipboardText(nint hwndOwner)
    {
        if (!Win32.OpenClipboard(hwndOwner)) return string.Empty;
        try
        {
            nint hMem = Win32.GetClipboardData(Win32.CF_UNICODETEXT);
            if (hMem == 0) return string.Empty;
            char* pText = (char*)Win32.GlobalLock(hMem);
            if (pText == null) return string.Empty;
            try
            {
                return new string(pText);
            }
            finally
            {
                Win32.GlobalUnlock(hMem);
            }
        }
        finally
        {
            Win32.CloseClipboard();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Utilities
    // ──────────────────────────────────────────────────────────────────────────
    public static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        if (bytes < 1024) return $"{bytes} B";
        string[] units = ["KB", "MB", "GB", "TB", "PB"];
        double d = bytes / 1024.0;
        int i = 0;
        while (d >= 1024.0 && i < units.Length - 1)
        {
            d /= 1024.0;
            i++;
        }
        return $"{d:0.##} {units[i]}";
    }

    public static string FormatComputationSpeed(double elapsedMs, long fileSize)
    {
        if (elapsedMs <= 0.001)
        {
            return "  ·  < 1毫秒";
        }

        if (elapsedMs < 1000.0)
        {
            if (elapsedMs < 1.0)
                return "  ·  < 1毫秒";
            return $"  ·  {Math.Round(elapsedMs)}毫秒";
        }

        double sec = elapsedMs / 1000.0;
        if (fileSize > 0)
        {
            double mb = fileSize / (1024.0 * 1024.0);
            double mbps = mb / sec;
            string speedFormatted = mbps >= 1024.0 ? $"{mbps / 1024.0:F1} GB/s" : $"{mbps:F1} MB/s";
            return $"  ·  {sec:F2}秒  ·  {speedFormatted}";
        }

        return $"  ·  {sec:F2}秒";
    }
}
