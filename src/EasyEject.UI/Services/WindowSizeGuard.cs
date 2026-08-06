using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EasyEject.UI.Services;

/// <summary>
/// Enforces a minimum window size by handling WM_GETMINMAXINFO.
/// </summary>
public static class WindowSizeGuard
{
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int GWLP_WNDPROC = -4;

    private static readonly Dictionary<IntPtr, IntPtr> OldProcs = new();
    private static readonly Dictionary<IntPtr, WndProcDelegate> ProcKeepAlives = new();
    private static int MinWidth;
    private static int MinHeight;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Applies a minimum width/height (in DIPs) to the given window.
    /// </summary>
    /// <param name="window">The window to constrain.</param>
    /// <param name="minWidth">Minimum width in DIPs.</param>
    /// <param name="minHeight">Minimum height in DIPs.</param>
    public static void SetMinSize(Window window, int minWidth, int minHeight)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        MinWidth = minWidth;
        MinHeight = minHeight;

        if (OldProcs.ContainsKey(hwnd))
        {
            return;
        }

        var proc = new WndProcDelegate(WndProc);
        IntPtr newProc = Marshal.GetFunctionPointerForDelegate(proc);
        IntPtr oldProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, newProc);

        ProcKeepAlives[hwnd] = proc;
        OldProcs[hwnd] = oldProc;
    }

    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            double scale = GetDpiForWindow(hwnd) / 96.0;
            minMax.PtMinTrackSize.X = (int)Math.Round(MinWidth * scale);
            minMax.PtMinTrackSize.Y = (int)Math.Round(MinHeight * scale);
            Marshal.StructureToPtr(minMax, lParam, false);
        }

        return OldProcs.TryGetValue(hwnd, out IntPtr oldProc)
            ? CallWindowProc(oldProc, hwnd, msg, wParam, lParam)
            : DefWindowProc(hwnd, msg, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point PtReserved;
        public Point PtMaxSize;
        public Point PtMaxPosition;
        public Point PtMinTrackSize;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);
}
