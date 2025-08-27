using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.UI.Xaml;

namespace HackPDM.Src.Helper
{
    public static partial class WindowHelper
    {
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

        public static void ResizeWindow(Window window, int width, int height)
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            MoveWindow(hwnd, 100, 100, width, height, true);
        }
    }
}
