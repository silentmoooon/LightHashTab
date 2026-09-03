using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LightHashTab.Hashing;
using LightHashTab.Interop;

namespace LightHashTab.UI;

public static class AlgorithmSettingsDialog
{
    private const int IDC_CHK_BASE = 5000;
    private const int IDC_BTN_ALL  = 5100;
    private const int IDC_BTN_DEF  = 5101;
    private const int IDOK         = 1;
    private const int IDCANCEL     = 2;

    private sealed class DialogState
    {
        public HashSet<HashAlgorithmType> Enabled = [];
        public readonly List<nint> CheckboxHwnds = [];
        public nint HFont;
        public nint HwndOwner;
    }

    public static unsafe bool Show(nint hwndOwner)
    {
        int templateSize = sizeof(DLGTEMPLATE) + 3 * sizeof(ushort);
        byte* pTemplate = (byte*)NativeMemory.AllocZeroed((nuint)templateSize);
        DLGTEMPLATE* dlg = (DLGTEMPLATE*)pTemplate;
        dlg->style = Win32.WS_POPUP | Win32.WS_CAPTION | Win32.WS_SYSMENU | Win32.DS_MODALFRAME | Win32.DS_CENTER;
        dlg->dwExtendedStyle = Win32.WS_EX_DLGMODALFRAME;
        dlg->cx = 260;
        dlg->cy = 220;

        var state = new DialogState
        {
            Enabled = AlgorithmConfig.LoadEnabledAlgorithms(),
            HwndOwner = hwndOwner
        };
        var handle = GCHandle.Alloc(state);

        try
        {
            nint res = Win32.DialogBoxIndirectParamW(
                0, dlg, hwndOwner, &SettingsDlgProc, GCHandle.ToIntPtr(handle));
            return res == 1;
        }
        finally
        {
            handle.Free();
            NativeMemory.Free(pTemplate);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe nint SettingsDlgProc(nint hwndDlg, uint uMsg, nuint wParam, nint lParam)
    {
        switch (uMsg)
        {
            case Win32.WM_INITDIALOG:
            {
                if (lParam == 0) return 1;
                var handle = GCHandle.FromIntPtr(lParam);
                if (!handle.IsAllocated || handle.Target is not DialogState state) return 1;
                Win32.SetWindowLongPtrW(hwndDlg, Win32.DWLP_USER, lParam);

                Win32.SetWindowTextW(hwndDlg, "算法设置");

                float scale = Win32.GetDpiScale(hwndDlg);

                // Get system font
                NONCLIENTMETRICSW ncm = default;
                ncm.cbSize = (uint)sizeof(NONCLIENTMETRICSW);
                nint hFont = 0;
                if (Win32.SystemParametersInfoW(Win32.SPI_GETNONCLIENTMETRICS, ncm.cbSize, &ncm, 0))
                    hFont = Win32.CreateFontIndirectW(&ncm.lfMessageFont);
                if (hFont == 0) hFont = Win32.GetStockObject(Win32.DEFAULT_GUI_FONT);
                state.HFont = hFont;

                int padX     = Win32.Scale(18, scale);
                int padY     = Win32.Scale(16, scale);
                int btnW     = Win32.Scale(68, scale);
                int btnH     = Win32.Scale(26, scale);
                int btnGap   = Win32.Scale(8, scale);
                int groupGap = Win32.Scale(36, scale);

                // Ensure client width is spacious enough for the 4 buttons with an explicit middle separator gap
                int minBtnRowW = padX * 2 + btnW * 4 + btnGap * 2 + groupGap;
                int clientW    = Math.Max(minBtnRowW, Win32.Scale(360, scale));

                int colGap  = Win32.Scale(16, scale);
                int colW    = (clientW - padX * 2 - colGap) / 2;
                int rowH    = Win32.Scale(24, scale);
                int titleH  = Win32.Scale(20, scale);

                // Title label
                nint hLbl = Win32.CreateWindowExW(
                    0, "STATIC", "选择要计算并显示的哈希算法:",
                    Win32.WS_CHILD | Win32.WS_VISIBLE,
                    padX, padY, clientW - padX * 2, titleH, hwndDlg, 0, 0, null);
                if (hFont != 0 && hLbl != 0) Win32.SendMessageW(hLbl, Win32.WM_SETFONT, (nuint)hFont, 1);

                // Create checkboxes for each algorithm in 2 columns
                int startY = padY + titleH + Win32.Scale(10, scale);
                int totalAlgos = AlgorithmConfig.AllAlgorithms.Length;
                int leftColCount = (totalAlgos + 1) / 2; // 5

                for (int i = 0; i < totalAlgos; i++)
                {
                    var (type, name) = AlgorithmConfig.AllAlgorithms[i];
                    int col = i < leftColCount ? 0 : 1;
                    int row = i < leftColCount ? i : i - leftColCount;

                    int x = padX + col * (colW + colGap);
                    int y = startY + row * rowH;

                    nint hChk = Win32.CreateWindowExW(
                        0, "BUTTON", name,
                        Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_AUTOCHECKBOX,
                        x, y, colW, rowH, hwndDlg, (nint)(IDC_CHK_BASE + i), 0, null);

                    if (hFont != 0 && hChk != 0) Win32.SendMessageW(hChk, Win32.WM_SETFONT, (nuint)hFont, 1);

                    if (state.Enabled.Contains(type))
                        Win32.SendMessageW(hChk, Win32.BM_SETCHECK, (nuint)Win32.BST_CHECKED, 0);

                    state.CheckboxHwnds.Add(hChk);
                }

                int checkAreaH = leftColCount * rowH;
                int btnY = startY + checkAreaH + Win32.Scale(16, scale);

                // Buttons: [全选] [默认]   ...   [确定] [取消]
                int btnAllX = padX;
                int btnDefX = btnAllX + btnW + btnGap;

                int btnCancelX = clientW - padX - btnW;
                int btnOkX     = btnCancelX - btnGap - btnW;

                nint hBtnAll = Win32.CreateWindowExW(
                    0, "BUTTON", "全选",
                    Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
                    btnAllX, btnY, btnW, btnH, hwndDlg, (nint)IDC_BTN_ALL, 0, null);

                nint hBtnDef = Win32.CreateWindowExW(
                    0, "BUTTON", "默认",
                    Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
                    btnDefX, btnY, btnW, btnH, hwndDlg, (nint)IDC_BTN_DEF, 0, null);

                nint hBtnOk = Win32.CreateWindowExW(
                    0, "BUTTON", "确定",
                    Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_DEFPUSHBUTTON,
                    btnOkX, btnY, btnW, btnH, hwndDlg, (nint)IDOK, 0, null);

                nint hBtnCancel = Win32.CreateWindowExW(
                    0, "BUTTON", "取消",
                    Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.WS_TABSTOP | Win32.BS_PUSHBUTTON,
                    btnCancelX, btnY, btnW, btnH, hwndDlg, (nint)IDCANCEL, 0, null);

                nint[] btns = [hBtnAll, hBtnDef, hBtnOk, hBtnCancel];
                foreach (var b in btns)
                    if (hFont != 0 && b != 0) Win32.SendMessageW(b, Win32.WM_SETFONT, (nuint)hFont, 1);

                int clientH = btnY + btnH + Win32.Scale(18, scale);

                // Resize window based on calculated client rect
                RECT rc = new RECT { Left = 0, Top = 0, Right = clientW, Bottom = clientH };
                uint style = (uint)Win32.GetWindowLongPtrW(hwndDlg, Win32.GWL_STYLE);
                Win32.AdjustWindowRectEx(&rc, style, false, Win32.WS_EX_DLGMODALFRAME);
                Win32.SetWindowPos(hwndDlg, 0, 0, 0, rc.Width, rc.Height, Win32.SWP_NOMOVE | Win32.SWP_NOZORDER);

                // Center window over parent property sheet
                Win32.CenterWindow(hwndDlg, state.HwndOwner);

                return 1;
            }

            case Win32.WM_COMMAND:
            {
                var state = GetState(hwndDlg);
                if (state == null) return 0;

                uint id = (uint)(wParam & 0xFFFF);
                if (id == IDC_BTN_ALL)
                {
                    foreach (var hChk in state.CheckboxHwnds)
                        Win32.SendMessageW(hChk, Win32.BM_SETCHECK, (nuint)Win32.BST_CHECKED, 0);
                    return 1;
                }
                else if (id == IDC_BTN_DEF)
                {
                    var defSet = new HashSet<HashAlgorithmType>(AlgorithmConfig.DefaultAlgorithms);
                    for (int i = 0; i < AlgorithmConfig.AllAlgorithms.Length && i < state.CheckboxHwnds.Count; i++)
                    {
                        var (type, _) = AlgorithmConfig.AllAlgorithms[i];
                        var check = defSet.Contains(type) ? Win32.BST_CHECKED : Win32.BST_UNCHECKED;
                        Win32.SendMessageW(state.CheckboxHwnds[i], Win32.BM_SETCHECK, (nuint)check, 0);
                    }
                    return 1;
                }
                else if (id == IDOK)
                {
                    var newEnabled = new HashSet<HashAlgorithmType>();
                    for (int i = 0; i < AlgorithmConfig.AllAlgorithms.Length && i < state.CheckboxHwnds.Count; i++)
                    {
                        var check = (int)Win32.SendMessageW(state.CheckboxHwnds[i], Win32.BM_GETCHECK, 0, 0);
                        if (check == Win32.BST_CHECKED)
                        {
                            newEnabled.Add(AlgorithmConfig.AllAlgorithms[i].Type);
                        }
                    }
                    if (newEnabled.Count == 0)
                    {
                        newEnabled.Add(HashAlgorithmType.Sha256);
                    }

                    AlgorithmConfig.SaveEnabledAlgorithms(newEnabled);
                    if (state.HFont != 0) Win32.DeleteObject(state.HFont);
                    Win32.EndDialog(hwndDlg, 1);
                    return 1;
                }
                else if (id == IDCANCEL)
                {
                    if (state.HFont != 0) Win32.DeleteObject(state.HFont);
                    Win32.EndDialog(hwndDlg, 0);
                    return 1;
                }
                break;
            }

            case Win32.WM_CLOSE:
            {
                var state = GetState(hwndDlg);
                if (state?.HFont != 0) Win32.DeleteObject(state!.HFont);
                Win32.EndDialog(hwndDlg, 0);
                return 1;
            }
        }

        return 0;
    }

    private static DialogState? GetState(nint hwndDlg)
    {
        if (hwndDlg == 0) return null;
        nint ptr = Win32.GetWindowLongPtrW(hwndDlg, Win32.DWLP_USER);
        if (ptr == 0) return null;
        var handle = GCHandle.FromIntPtr(ptr);
        return handle.IsAllocated ? handle.Target as DialogState : null;
    }
}
