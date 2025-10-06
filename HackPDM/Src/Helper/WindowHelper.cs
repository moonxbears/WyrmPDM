using System;
using System.Numerics;
using System.Runtime.InteropServices;

using HackPDM.Src.ClientUtils.Types;
using HackPDM.Src.Data.Numeric;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HackPDM.Helper;

public static partial class WindowHelper
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

    public static void ResizeWindow(Window window, int width, int height)
    {
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        MoveWindow(hwnd, 100, 100, width, height, true);
    }

    public static Window CreateWindowPage(Type pageType)
    {
        var window = new MainWindow();
        var rootFrame = new Frame();
        window.Activate();
        window.Content = rootFrame;
        rootFrame.Navigate(pageType);
        return window;
    }
    public static Window CreateWindowPage<T>() where T : Page
    {
        var window = new MainWindow();
        var rootFrame = new Frame();
        window.Activate();
        window.Content = rootFrame;
        string name = typeof(T).Name;
        if (StorageBox.PresetWindowConfig.TryGetValue(name, out WindowConfig? value))
        {
            SetWindowConfig(window, value);
        }
        rootFrame.Navigate(typeof(T));
        Page? page = rootFrame.Content as Page;
        if (page != null)
        {
            InstanceManager.RegisterWindow(page, window);
        }
        return window;
    }
    public static Window CreateWindowPage<T>(WindowConfig winConfig) where T : Page
    {
        Window win = CreateWindowPage<T>();
        SetWindowConfig(win, winConfig);
        return win;
    }
    private static void SetWindowConfig(Window window, WindowConfig windowConfig)
    {
        window.AppWindow.MoveAndResize(windowConfig.PositionAndSize);
        window.Title = windowConfig.Title;
    }
}
public class WindowConfig(string title, int4 positionAndSize)
{
    public string Title { get; set; } = title;
    public int4 PositionAndSize { get; set; } = positionAndSize;
}