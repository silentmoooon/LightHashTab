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
    private const int IDC_FILE_INFO = 2001;
    private const int IDC_LIST_HASHES = 2002;
    private const int IDC_PROGRESS = 2003;
    private const int IDC_LABEL_COMPARE = 2004;
    private const int IDC_EDIT_COMPARE = 2005;
    private const int IDC_LABEL_MATCH = 2006;
    private const int IDC_BTN_COPY_SEL = 2007;
    private const int IDC_BTN_COPY_ALL = 2008;

    public sealed class PageState
    {
        public required List<string> FilePaths { get; init; }
        public required FileHashSummary Summary { get; init; }
        public CancellationTokenSource? Cts { get; set; }
        public GCHandle GcHandle { get; set; }

        public nint PTemplate { get; set; }
        public nint PTitle { get; set; }

        public nint HwndDlg { get; set; }
        public nint HwndFileInfo { get; set; }
        public nint HwndListView { get; set; }
        public nint HwndProgress { get; set; }
        public nint HwndLabelCompare { get; set; }
        public nint HwndEditCompare { get; set; }
        public nint HwndLabelMatch { get; set; }
        public nint HwndBtnCopySel { get; set; }
        public nint HwndBtnCopyAll { get; set; }

        public void StartCalculation()
        {
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
                        filePath,
                        hashList,
                        onProgress: percent =>
                        {
                            if (hwnd != 0)
                            {
                                Win32.PostMessageW(hwnd, Win32.WM_APP_PROGRESS, (nuint)percent, 0);
                            }
                        },
                        token).ConfigureAwait(false);

                    if (hwnd != 0)
                    {
                        Win32.PostMessageW(hwnd, Win32.WM_APP_HASH_FINISHED, 0, 0);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Task canceled
                }
                catch (Exception ex)
                {
                    foreach (var h in hashList)
                    {
                        h.Status = $"Error: {ex.Message}";
                    }
                    if (hwnd != 0)
                    {
                        Win32.PostMessageW(hwnd, Win32.WM_APP_HASH_FINISHED, 0, 0);
                    }
                }
            }, token);
        }
    }

    public static unsafe nint CreatePage(List<string> filePaths)
    {
        if (filePaths.Count == 0)
        {
            return 0;
        }

        string firstFile = filePaths[0];
        long fileSize = 0;
        try
        {
            if (File.Exists(firstFile))
            {
                fileSize = new FileInfo(firstFile).Length;
            }
        }
        catch { }

        var summary = new FileHashSummary
        {
            FilePath = firstFile,
            FileName = Path.GetFileName(firstFile),
            FileSize = fileSize,
            Hashes =
            [
                new HashItem { Type = HashAlgorithmType.Blake3, Name = "BLAKE3" },
                new HashItem { Type = HashAlgorithmType.Sha256, Name = "SHA-256" },
                new HashItem { Type = HashAlgorithmType.Sha512, Name = "SHA-512" },
                new HashItem { Type = HashAlgorithmType.Sha1, Name = "SHA-1" },
                new HashItem { Type = HashAlgorithmType.Md5, Name = "MD5" },
                new HashItem { Type = HashAlgorithmType.Crc32, Name = "CRC32" },
                new HashItem { Type = HashAlgorithmType.Xxh64, Name = "XXH64" }
            ]
        };

        var state = new PageState
        {
            FilePaths = filePaths,
            Summary = summary
        };

        var gcHandle = GCHandle.Alloc(state);
        state.GcHandle = gcHandle;

        // Allocate in-memory DLGTEMPLATE in unmanaged memory
        int templateSize = sizeof(DLGTEMPLATE) + 3 * sizeof(ushort);
        byte* pTemplate = (byte*)NativeMemory.AllocZeroed((nuint)templateSize);
        DLGTEMPLATE* dlg = (DLGTEMPLATE*)pTemplate;
        dlg->style = Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.DS_CONTROL;
        dlg->dwExtendedStyle = 0x00010000; // WS_EX_CONTROLPARENT
        dlg->cdit = 0;
        dlg->cx = 240;
        dlg->cy = 200;
        state.PTemplate = (nint)pTemplate;

        // Allocate Title in unmanaged memory so it NEVER gets relocated or collected by GC
        nint pTitle = Marshal.StringToHGlobalUni("File Hashes");
        state.PTitle = pTitle;

        PROPSHEETPAGEW psp = new()
        {
            dwSize = (uint)sizeof(PROPSHEETPAGEW),
            dwFlags = Win32.PSP_DLGINDIRECT | Win32.PSP_USETITLE | Win32.PSP_USECALLBACK,
            hInstance = 0,
            pszTemplate = (char*)pTemplate,
            pszTitle = (char*)pTitle,
            pfnDlgProc = &DialogProc,
            lParam = GCHandle.ToIntPtr(gcHandle),
            pfnCallback = &PropSheetPageCallback
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
        if (uMsg == Win32.PSPCB_RELEASE)
        {
            if (ppsp != null && ppsp->lParam != 0)
            {
                var gcHandle = GCHandle.FromIntPtr(ppsp->lParam);
                if (gcHandle.IsAllocated && gcHandle.Target is PageState state)
                {
                    state.Cts?.Cancel();
                    if (state.PTemplate != 0)
                    {
                        NativeMemory.Free((void*)state.PTemplate);
                        state.PTemplate = 0;
                    }
                    if (state.PTitle != 0)
                    {
                        Marshal.FreeHGlobal(state.PTitle);
                        state.PTitle = 0;
                    }
                    gcHandle.Free();
                }
            }
        }
        return 1;
    }

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

                // Initialize common controls
                INITCOMMONCONTROLSEX icc = new()
                {
                    dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
                    dwICC = Win32.ICC_LISTVIEW_CLASSES | Win32.ICC_PROGRESS_CLASS | Win32.ICC_STANDARD_CLASSES
                };
                Win32.InitCommonControlsEx(&icc);

                InitializeDialogControls(state);
                state.StartCalculation();
                return 1;
            }

            case Win32.WM_NOTIFY:
            {
                NMHDR* pnm = (NMHDR*)lParam;
                if (pnm != null)
                {
                    if (pnm->code == Win32.PSN_SETACTIVE)
                    {
                        Win32.SetWindowLongPtrW(hwndDlg, Win32.DWLP_MSGRESULT, 0);
                        return 1;
                    }
                    else if (pnm->code == Win32.PSN_KILLACTIVE || pnm->code == Win32.PSN_APPLY)
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
                if (state != null)
                {
                    LayoutControls(state);
                }
                return 0;
            }

            case Win32.WM_COMMAND:
            {
                var state = GetState(hwndDlg);
                if (state != null)
                {
                    uint id = (uint)(wParam & 0xFFFF);
                    uint code = (uint)((wParam >> 16) & 0xFFFF);

                    if (id == IDC_EDIT_COMPARE && code == 0x0300 /* EN_CHANGE */)
                    {
                        CheckHashMatch(state);
                    }
                    else if (id == IDC_BTN_COPY_SEL)
                    {
                        CopySelectedHash(state);
                    }
                    else if (id == IDC_BTN_COPY_ALL)
                    {
                        CopyAllHashes(state);
                    }
                }
                return 0;
            }

            case Win32.WM_APP_PROGRESS:
            {
                var state = GetState(hwndDlg);
                if (state != null && state.HwndProgress != 0)
                {
                    int percent = (int)wParam;
                    Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETPOS, (nuint)percent, 0);
                }
                return 0;
            }

            case Win32.WM_APP_HASH_FINISHED:
            {
                var state = GetState(hwndDlg);
                if (state != null)
                {
                    UpdateListViewItems(state);
                    if (state.HwndProgress != 0)
                    {
                        Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETPOS, 100, 0);
                    }
                    CheckHashMatch(state);
                }
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
        nint hwnd = state.HwndDlg;
        nint hFont = Win32.GetStockObject(Win32.DEFAULT_GUI_FONT);

        // File info summary text
        string sizeStr = FormatFileSize(state.Summary.FileSize);
        string infoText = $"{state.Summary.FileName} ({sizeStr})";

        state.HwndFileInfo = Win32.CreateWindowExW(
            0, "STATIC", infoText,
            Win32.WS_CHILD | Win32.WS_VISIBLE,
            8, 8, 300, 18,
            hwnd, (nint)IDC_FILE_INFO, 0, null);

        // List View (Hashes table)
        state.HwndListView = Win32.CreateWindowExW(
            0, "SysListView32", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_BORDER | Win32.LVS_REPORT | Win32.LVS_SINGLESEL | Win32.LVS_SHOWSELALWAYS,
            8, 30, 320, 140,
            hwnd, (nint)IDC_LIST_HASHES, 0, null);

        if (state.HwndListView != 0)
        {
            Win32.SendMessageW(state.HwndListView, Win32.LVM_SETEXTENDEDLISTVIEWSTYLE, 0,
                (nint)(Win32.LVS_EX_FULLROWSELECT | Win32.LVS_EX_GRIDLINES | Win32.LVS_EX_DOUBLEBUFFER));

            ThemeHelper.ApplyTheme(state.HwndListView);

            // Add Columns
            AddColumn(state.HwndListView, 0, "Algorithm", 85);
            AddColumn(state.HwndListView, 1, "Hash Value", 260);
            AddColumn(state.HwndListView, 2, "Status", 75);

            // Populate initial rows
            for (int i = 0; i < state.Summary.Hashes.Count; i++)
            {
                var h = state.Summary.Hashes[i];
                InsertRow(state.HwndListView, i, h.Name, "Computing...", h.Status);
            }
        }

        // Progress bar
        state.HwndProgress = Win32.CreateWindowExW(
            0, "msctls_progress32", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.PBS_SMOOTH,
            8, 175, 320, 12,
            hwnd, (nint)IDC_PROGRESS, 0, null);

        if (state.HwndProgress != 0)
        {
            Win32.SendMessageW(state.HwndProgress, Win32.PBM_SETRANGE32, 0, 100);
        }

        // Compare Row: Label + Edit Box
        state.HwndLabelCompare = Win32.CreateWindowExW(
            0, "STATIC", "Compare:",
            Win32.WS_CHILD | Win32.WS_VISIBLE,
            8, 195, 60, 20,
            hwnd, (nint)IDC_LABEL_COMPARE, 0, null);

        state.HwndEditCompare = Win32.CreateWindowExW(
            0, "EDIT", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_BORDER | Win32.ES_AUTOHSCROLL | Win32.WS_TABSTOP,
            72, 193, 256, 22,
            hwnd, (nint)IDC_EDIT_COMPARE, 0, null);

        // Match status label
        state.HwndLabelMatch = Win32.CreateWindowExW(
            0, "STATIC", "",
            Win32.WS_CHILD | Win32.WS_VISIBLE,
            8, 222, 150, 22,
            hwnd, (nint)IDC_LABEL_MATCH, 0, null);

        // Action buttons
        state.HwndBtnCopySel = Win32.CreateWindowExW(
            0, "BUTTON", "Copy Selected",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            160, 220, 95, 24,
            hwnd, (nint)IDC_BTN_COPY_SEL, 0, null);

        state.HwndBtnCopyAll = Win32.CreateWindowExW(
            0, "BUTTON", "Copy All",
            Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
            260, 220, 75, 24,
            hwnd, (nint)IDC_BTN_COPY_ALL, 0, null);

        // Set GUI font on all controls
        nint[] controls =
        [
            state.HwndFileInfo, state.HwndListView, state.HwndProgress,
            state.HwndLabelCompare, state.HwndEditCompare, state.HwndLabelMatch,
            state.HwndBtnCopySel, state.HwndBtnCopyAll
        ];

        foreach (var c in controls)
        {
            if (c != 0)
            {
                Win32.SendMessageW(c, Win32.WM_SETFONT, (nuint)hFont, 1);
            }
        }

        LayoutControls(state);
    }

    private static void LayoutControls(PageState state)
    {
        if (state.HwndDlg == 0) return;

        Win32.GetClientRect(state.HwndDlg, out RECT rect);
        int w = rect.Width;
        int h = rect.Height;
        if (w <= 0 || h <= 0) return;

        const int pad = 8;
        int contentW = w - pad * 2;

        // Top info: y = 8, h = 18
        if (state.HwndFileInfo != 0)
        {
            Win32.SetWindowPos(state.HwndFileInfo, 0, pad, pad, contentW, 18, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }

        // Bottom section height is approx 88px
        int listY = 30;
        int listH = Math.Max(50, h - listY - 88);
        if (state.HwndListView != 0)
        {
            Win32.SetWindowPos(state.HwndListView, 0, pad, listY, contentW, listH, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }

        int progressY = listY + listH + 6;
        if (state.HwndProgress != 0)
        {
            Win32.SetWindowPos(state.HwndProgress, 0, pad, progressY, contentW, 10, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }

        int compareY = progressY + 16;
        int compareLabelW = 60;
        if (state.HwndLabelCompare != 0)
        {
            Win32.SetWindowPos(state.HwndLabelCompare, 0, pad, compareY + 2, compareLabelW, 18, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }
        if (state.HwndEditCompare != 0)
        {
            Win32.SetWindowPos(state.HwndEditCompare, 0, pad + compareLabelW + 4, compareY, Math.Max(10, contentW - compareLabelW - 4), 22, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }

        int actionsY = compareY + 26;
        int btnW1 = 100;
        int btnW2 = 80;
        int matchLabelW = Math.Max(60, contentW - btnW1 - btnW2 - 12);

        if (state.HwndLabelMatch != 0)
        {
            Win32.SetWindowPos(state.HwndLabelMatch, 0, pad, actionsY + 2, matchLabelW, 20, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }
        if (state.HwndBtnCopySel != 0)
        {
            Win32.SetWindowPos(state.HwndBtnCopySel, 0, pad + matchLabelW + 4, actionsY, btnW1, 23, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }
        if (state.HwndBtnCopyAll != 0)
        {
            Win32.SetWindowPos(state.HwndBtnCopyAll, 0, pad + matchLabelW + 4 + btnW1 + 4, actionsY, btnW2, 23, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }
    }

    private static unsafe void AddColumn(nint hwndList, int index, string text, int width)
    {
        if (hwndList == 0) return;
        fixed (char* pText = text)
        {
            LVCOLUMNW col = new()
            {
                mask = Win32.LVCF_TEXT | Win32.LVCF_WIDTH | Win32.LVCF_FMT,
                fmt = Win32.LVCFMT_LEFT,
                cx = width,
                pszText = pText
            };
            Win32.SendMessageW(hwndList, Win32.LVM_INSERTCOLUMNW, (nuint)index, (nint)(&col));
        }
    }

    private static unsafe void InsertRow(nint hwndList, int row, string algorithm, string hash, string status)
    {
        if (hwndList == 0) return;
        fixed (char* pAlgo = algorithm)
        {
            LVITEMW item = new()
            {
                mask = Win32.LVIF_TEXT,
                iItem = row,
                iSubItem = 0,
                pszText = pAlgo
            };
            Win32.SendMessageW(hwndList, Win32.LVM_INSERTITEMW, 0, (nint)(&item));
        }

        SetRowSubItem(hwndList, row, 1, hash);
        SetRowSubItem(hwndList, row, 2, status);
    }

    private static unsafe void SetRowSubItem(nint hwndList, int row, int subItem, string text)
    {
        if (hwndList == 0) return;
        fixed (char* pText = text)
        {
            LVITEMW item = new()
            {
                mask = Win32.LVIF_TEXT,
                iItem = row,
                iSubItem = subItem,
                pszText = pText
            };
            Win32.SendMessageW(hwndList, Win32.LVM_SETITEMW, 0, (nint)(&item));
        }
    }

    private static void UpdateListViewItems(PageState state)
    {
        if (state.HwndListView == 0) return;
        for (int i = 0; i < state.Summary.Hashes.Count; i++)
        {
            var h = state.Summary.Hashes[i];
            SetRowSubItem(state.HwndListView, i, 1, h.Value);
            SetRowSubItem(state.HwndListView, i, 2, h.Status);
        }
    }

    private static unsafe void CheckHashMatch(PageState state)
    {
        if (state.HwndEditCompare == 0 || state.HwndLabelMatch == 0) return;

        int length = Win32.GetWindowTextLengthW(state.HwndEditCompare);
        if (length == 0)
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, string.Empty);
            return;
        }

        char* pBuf = stackalloc char[length + 2];
        int read = Win32.GetWindowTextW(state.HwndEditCompare, pBuf, length + 1);
        string input = read > 0 ? new string(pBuf, 0, read).Trim() : string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, string.Empty);
            return;
        }

        HashItem? matched = null;

        for (int i = 0; i < state.Summary.Hashes.Count; i++)
        {
            var h = state.Summary.Hashes[i];
            if (!string.IsNullOrEmpty(h.Value) && string.Equals(h.Value, input, StringComparison.OrdinalIgnoreCase))
            {
                matched = h;
                break;
            }
        }

        if (matched != null)
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, $"Match: {matched.Name}");
        }
        else
        {
            Win32.SetWindowTextW(state.HwndLabelMatch, "No match");
        }
    }

    private static void CopySelectedHash(PageState state)
    {
        if (state.HwndListView == 0) return;
        int selectedIndex = (int)Win32.SendMessageW(state.HwndListView, Win32.LVM_GETNEXTITEM, unchecked((nuint)(-1)), Win32.LVNI_SELECTED);
        if (selectedIndex >= 0 && selectedIndex < state.Summary.Hashes.Count)
        {
            string hashValue = state.Summary.Hashes[selectedIndex].Value;
            if (!string.IsNullOrEmpty(hashValue))
            {
                CopyToClipboard(state.HwndDlg, hashValue);
            }
        }
    }

    private static void CopyAllHashes(PageState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"File: {state.Summary.FileName}");
        sb.AppendLine($"Size: {state.Summary.FileSize} bytes ({FormatFileSize(state.Summary.FileSize)})");
        sb.AppendLine("--------------------------------------------------");
        foreach (var h in state.Summary.Hashes)
        {
            sb.AppendLine($"{h.Name,-10} {h.Value}");
        }

        CopyToClipboard(state.HwndDlg, sb.ToString());
    }

    private static unsafe void CopyToClipboard(nint hwndOwner, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!Win32.OpenClipboard(hwndOwner)) return;

        try
        {
            Win32.EmptyClipboard();
            int byteCount = (text.Length + 1) * sizeof(char);
            nint hMem = Win32.GlobalAlloc(Win32.GMEM_MOVEABLE, (nuint)byteCount);
            if (hMem == 0) return;

            void* pMem = Win32.GlobalLock(hMem);
            if (pMem != null)
            {
                fixed (char* pText = text)
                {
                    Buffer.MemoryCopy(pText, pMem, byteCount, byteCount);
                }
                Win32.GlobalUnlock(hMem);
                Win32.SetClipboardData(Win32.CF_UNICODETEXT, hMem);
            }
        }
        finally
        {
            Win32.CloseClipboard();
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
