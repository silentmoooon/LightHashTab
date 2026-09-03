using System;
using System.Runtime.InteropServices;
using LightHashTab.Interop;

namespace LightHashTab.UI;

public static class ThemeHelper
{
    public static void ApplyTheme(nint hwnd)
    {
        try
        {
            // Apply standard modern Explorer visual style
            Win32.SetWindowTheme(hwnd, "Explorer", null);
        }
        catch
        {
            // Ignore if uxtheme is unavailable
        }
    }

    public static bool IsDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
            {
                return val == 0;
            }
        }
        catch { }
        return false;
    }
}

