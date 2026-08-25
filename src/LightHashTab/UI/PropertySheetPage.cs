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
    private const int IDC_FILE_INFO    = 2001;
    private const int IDC_LIST_HASHES  = 2002;
    private const int IDC_PROGRESS     = 2003;
    private const int IDC_EDIT_COMPARE = 2004;
    private const int IDC_LABEL_MATCH  = 2005;
    private const int IDC_BTN_COPY_SEL = 2006;
    private const int IDC_BTN_COPY_ALL = 2007;

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
        public nint HFont      { get; set; }   // Segoe UI font (owned)

        public nint HwndDlg          { get; set; }
        public nint HwndFileInfo     { get; set; }
        public nint HwndListView     { get; set; }
        public nint HwndProgress     { get; set; }
        public nint HwndEditCompare  { get; set; }
        public nint HwndLabelMatch   { get; set; }
        public nint HwndBtnCopySel   { get; set; }
        public nint HwndBtnCopyAll   { get; set; }

        public bool IsComputing { get; set; } = true;
        public int  MatchedRow  { get; set; } = -1;

        public void StartCalculation()
        {
            IsComputing = true;
            MatchedRow = -1;

            Cts?.Cancel();
            Cts = new CancellationTokenSource();
            var token = Cts.Token;
            var hwnd = HwndDlg;
            var filePath = Summary.FilePath;
            var hashList = Summary.Hashes;

            Task.Run(async () =>
            {
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

                    if (hwnd != 0)
                        Win32.PostMessageW(hwnd, Win32.WM_APP_HASH_FINISHED, 0, 0);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
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
        // Fallback: DEFAULT_GUI_FONT
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
            Hashes =
            [
                new HashItem { Type = HashAlgorithmType.Blake3,  Name = "BLAKE3"  },
                new HashItem { Type = HashAlgorithmType.Sha256,  Name = "SHA-256" },
                new HashItem { Type = HashAlgorithmType.Sha512,  Name = "SHA-512" },
                new HashItem { Type = HashAlgorithmType.Sha1,    Name = "SHA-1"   },
                new HashItem { Type = HashAlgorithmType.Md5,     Name = "MD5"     },
                new HashItem { Type = HashAlgorithmType.Crc32,   Name = "CRC32"   },
                new HashItem { Type = HashAlgorithmType.Xxh64,   Name = "XXH64"   },
            ]
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

        nint pTitle = Marshal.StringToHGlobalUni("File Hashes");
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
                else if (id == IDC_BTN_COPY_SEL) CopySelectedHash(state);
                else if (id == IDC_BTN_COPY_ALL) CopyAllHashes(state);
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

                // Switch progress bar from marquee to 100%
                if (state.HwndProgress != 0)
                {
                    // Remove marquee style, set to 100
                    uint curStyle = (uint)Win32.GetWindowLongPtrW(state.HwndProgress, Win32.GWL_STYLE);
                    Win32.SetWindowLongPtrW(state.HwndProgress, Win32.GWL_STYLE,
                        (nint)(curStyle & ~Win32.PBS_MARQUEE | Win32.PBS_SMOOTH));
                    Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETMARQUEE, 0, 0);
                    Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETRANGE32, 0, 100);
                    Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETPOS, 100, 0);
                }

                UpdateListViewItems(state);
                CheckHashMatch(state);
                return 0;
            }

            case Win32.WM_DESTROY:
            {
                var state = GetState(hwndDlg);
                if (state != null)
                {
                    state.Cts?.Cancel();
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

        // ── File info label ───────────────────────────────────────────────────
        string sizeStr  = FormatFileSize(state.Summary.FileSize);
        string infoText = $"{state.Summary.FileName}  ({sizeStr})";
        state.HwndFileInfo = Win32.CreateWindowExW(
            0, "STATIC", infoText,
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.SS_ENDELLIPSIS,
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
                (nint)(Win32.LVS_EX_FULLROWSELECT | Win32.LVS_EX_DOUBLEBUFFER));
            ThemeHelper.ApplyTheme(state.HwndListView);

            // Initial column widths - will be auto-resized in LayoutControls
            AddColumn(state.HwndListView, COL_ALGO, "Algorithm", 92);
            AddColumn(state.HwndListView, COL_HASH, "Hash Value", 300);

            for (int i = 0; i < state.Summary.Hashes.Count; i++)
                InsertRow(state.HwndListView, i, state.Summary.Hashes[i].Name, "Computing…");
        }

        // ── Progress bar (starts in Marquee mode) ─────────────────────────────
        state.HwndProgress = Win32.CreateWindowExW(
            0, "msctls_progress32", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.PBS_MARQUEE,
            0, 0, 300, 6, hwnd, (nint)IDC_PROGRESS, 0, null);

        if (state.HwndProgress != 0)
        {
            ThemeHelper.ApplyTheme(state.HwndProgress);
            Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETMARQUEE, 1, 40);
        }

        // ── Compare edit box (full width, with placeholder text) ──────────────
        state.HwndEditCompare = Win32.CreateWindowExW(
            0, "EDIT", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_BORDER
            | Win32.ES_AUTOHSCROLL | Win32.WS_TABSTOP,
            0, 0, 300, 23, hwnd, (nint)IDC_EDIT_COMPARE, 0, null);

        if (state.HwndEditCompare != 0)
        {
            // Set placeholder/cue text shown when edit is empty
            fixed (char* pCue = "Paste hash here to compare…")
                Win32.SendMessageW(state.HwndEditCompare, Win32.EM_SETCUEBANNER, 1, (nint)pCue);
        }

        // ── Match result label (left-aligned) ─────────────────────────────────
        state.HwndLabelMatch = Win32.CreateWindowExW(
            0, "STATIC", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.SS_ENDELLIPSIS,
            0, 0, 150, 22, hwnd, (nint)IDC_LABEL_MATCH, 0, null);

        // ── Copy buttons ──────────────────────────────────────────────────────
        state.HwndBtnCopySel = Win32.CreateWindowExW(
            0, "BUTTON", "Copy",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            0, 0, 60, 23, hwnd, (nint)IDC_BTN_COPY_SEL, 0, null);

        state.HwndBtnCopyAll = Win32.CreateWindowExW(
            0, "BUTTON", "Copy All",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            0, 0, 76, 23, hwnd, (nint)IDC_BTN_COPY_ALL, 0, null);

        // ── Apply font to all controls ────────────────────────────────────────
        nint[] controls =
        [
            state.HwndFileInfo, state.HwndListView,
            state.HwndEditCompare, state.HwndLabelMatch,
            state.HwndBtnCopySel, state.HwndBtnCopyAll
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

        const int pad   = 8;
        const int gap   = 5;
        const int editH = 23;
        const int btnH  = 23;
        const int progH = 5;
        const int infoH = 18;

        // Button widths — fixed, independent of dialog width
        const int btnCopyW    = 60;   // "Copy"
        const int btnCopyAllW = 76;   // "Copy All"

        int contentW = w - pad * 2;

        // ── Row positions from bottom ──────────────────────────────────────────
        // [pad] [btnRow] [gap] [editRow] [gap] [progress] [gap] [listview] [gap] [fileInfo] [pad]

        int btnRowBottom  = h - pad;
        int btnRowTop     = btnRowBottom - btnH;
        int editRowBottom = btnRowTop - gap;
        int editRowTop    = editRowBottom - editH;
        int progressBottom = editRowTop - gap;
        int progressTop    = progressBottom - progH;
        int listBottom     = progressTop - gap;
        int listTop        = pad + infoH + gap;
        int listH          = Math.Max(30, listBottom - listTop);

        // ── File info label ───────────────────────────────────────────────────
        if (state.HwndFileInfo != 0)
            Win32.SetWindowPos(state.HwndFileInfo, 0,
                pad, pad, contentW, infoH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        // ── List View ────────────────────────────────────────────────────────
        if (state.HwndListView != 0)
        {
            Win32.SetWindowPos(state.HwndListView, 0,
                pad, listTop, contentW, listH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

            // Auto-size Algorithm column, give the rest to Hash Value
            const int algoColW = 92;
            int hashColW = Math.Max(60, contentW - algoColW - 22); // 22 = scrollbar allowance
            Win32.SendMessageW(state.HwndListView, Win32.LVM_SETCOLUMNWIDTH, COL_ALGO, algoColW);
            Win32.SendMessageW(state.HwndListView, Win32.LVM_SETCOLUMNWIDTH, COL_HASH, hashColW);
        }

        // ── Progress bar ─────────────────────────────────────────────────────
        if (state.HwndProgress != 0)
            Win32.SetWindowPos(state.HwndProgress, 0,
                pad, progressTop, contentW, progH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        // ── Compare edit box (full width) ─────────────────────────────────────
        if (state.HwndEditCompare != 0)
            Win32.SetWindowPos(state.HwndEditCompare, 0,
                pad, editRowTop, contentW, editH,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);

        // ── Button row: [match label ...........] [Copy] [gap] [Copy All] ─────
        int rightEdge = pad + contentW;
        int copyAllX  = rightEdge - btnCopyAllW;
        int copySelX  = copyAllX - gap - btnCopyW;
        int matchLabelW = Math.Max(10, copySelX - pad - gap);

        if (state.HwndLabelMatch != 0)
            Win32.SetWindowPos(state.HwndLabelMatch, 0,
                pad, btnRowTop + 2, matchLabelW, btnH,
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
    // Compare / Match
    // ──────────────────────────────────────────────────────────────────────────
    private static unsafe void CheckHashMatch(PageState state)
    {
        if (state.HwndEditCompare == 0 || state.HwndLabelMatch == 0) return;

        int length = Win32.GetWindowTextLengthW(state.HwndEditCompare);
        if (length == 0)
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, "");
            state.MatchedRow = -1;
            return;
        }

        char* pBuf = stackalloc char[length + 2];
        int read = Win32.GetWindowTextW(state.HwndEditCompare, pBuf, length + 1);
        string input = read > 0 ? new string(pBuf, 0, read).Trim() : string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, "");
            state.MatchedRow = -1;
            return;
        }

        if (state.IsComputing)
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, "Still computing…");
            return;
        }

        int matchRow = -1;
        string matchName = "";
        for (int i = 0; i < state.Summary.Hashes.Count; i++)
        {
            var h = state.Summary.Hashes[i];
            if (!string.IsNullOrEmpty(h.Value) &&
                string.Equals(h.Value, input, StringComparison.OrdinalIgnoreCase))
            {
                matchRow = i;
                matchName = h.Name;
                break;
            }
        }

        state.MatchedRow = matchRow;
        Win32.SetWindowTextW(state.HwndLabelMatch,
            matchRow >= 0 ? $"✓  Match: {matchName}" : "✗  No match");
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
            if (!string.IsNullOrEmpty(v)) CopyToClipboard(state.HwndDlg, v);
        }
    }

    private static void CopyAllHashes(PageState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"File : {state.Summary.FileName}");
        sb.AppendLine($"Size : {state.Summary.FileSize} bytes  ({FormatFileSize(state.Summary.FileSize)})");
        sb.AppendLine(new string('-', 64));
        foreach (var h in state.Summary.Hashes)
            sb.AppendLine($"{h.Name,-8}  {h.Value}");
        CopyToClipboard(state.HwndDlg, sb.ToString());
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

    // ──────────────────────────────────────────────────────────────────────────
    // Utilities
    // ──────────────────────────────────────────────────────────────────────────
    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double d = bytes;
        string[] units = ["KB", "MB", "GB", "TB"];
        int i = 0;
        while (d >= 1024 && i < units.Length - 1) { d /= 1024; i++; }
        return $"{d:0.##} {units[i]}";
    }
}
