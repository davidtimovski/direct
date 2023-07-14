using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Chat.Utilities;

internal static class WindowingUtil
{
    public static SizeInt32 GetSize(Window window)
    {
        AppWindow appWindow = GetAppWindow(window);
        return appWindow.Size;
    }

    public static void Resize(Window window, SizeInt32 size)
    {
        AppWindow appWindow = GetAppWindow(window);
        appWindow.Resize(size);
    }

    // Hacky hack
    // https://github.com/microsoft/microsoft-ui-xaml/issues/2731#issuecomment-1014811559
    private static AppWindow GetAppWindow(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }
}
