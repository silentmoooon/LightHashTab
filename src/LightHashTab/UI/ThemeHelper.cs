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
}
