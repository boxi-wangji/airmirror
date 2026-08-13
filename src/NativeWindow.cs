using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace AirMirror;

internal static class NativeWindow
{
    private const int DwmaUseImmersiveDarkMode = 20;
    private const int DwmaBorderColor = 34;
    private const int DwmaCaptionColor = 35;
    private const int DwmaTextColor = 36;
    private const uint WmClose = 0x0010;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    internal static void ApplyBlackTitleBar(IntPtr windowHandle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        var enabled = 1;
        // Windows COLORREF uses BGR order: #05070B becomes 0x0B0705.
        var titleBarColor = 0x0B0705;
        var white = 0xFFFFFF;
        var size = sizeof(int);

        _ = DwmSetWindowAttribute(windowHandle, DwmaUseImmersiveDarkMode, ref enabled, size);
        _ = DwmSetWindowAttribute(windowHandle, DwmaBorderColor, ref titleBarColor, size);
        _ = DwmSetWindowAttribute(windowHandle, DwmaCaptionColor, ref titleBarColor, size);
        _ = DwmSetWindowAttribute(windowHandle, DwmaTextColor, ref white, size);
    }

    internal static IntPtr FindVisibleWindow(int processId, string titleFragment)
    {
        var foundWindow = IntPtr.Zero;

        EnumWindows((windowHandle, _) =>
        {
            GetWindowThreadProcessId(windowHandle, out var windowProcessId);
            if (windowProcessId != processId || !IsWindowVisible(windowHandle))
            {
                return true;
            }

            var titleLength = GetWindowTextLength(windowHandle);
            if (titleLength == 0)
            {
                return true;
            }

            var title = new StringBuilder(titleLength + 1);
            GetWindowText(windowHandle, title, title.Capacity);
            if (!title.ToString().Contains(titleFragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foundWindow = windowHandle;
            return false;
        }, IntPtr.Zero);

        return foundWindow;
    }

    internal static bool TryGetClientSize(IntPtr windowHandle, out Size size)
    {
        size = Size.Empty;
        if (!GetClientRect(windowHandle, out var clientBounds))
        {
            return false;
        }

        size = new Size(clientBounds.Width, clientBounds.Height);
        return size.Width > 0 && size.Height > 0;
    }

    internal static bool ResizeClientArea(IntPtr windowHandle, Size targetClientSize)
    {
        if (!GetClientRect(windowHandle, out var currentClientBounds) ||
            !GetWindowRect(windowHandle, out var currentWindowBounds))
        {
            return false;
        }

        var targetWindowWidth = currentWindowBounds.Width + targetClientSize.Width - currentClientBounds.Width;
        var targetWindowHeight = currentWindowBounds.Height + targetClientSize.Height - currentClientBounds.Height;

        return SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            currentWindowBounds.Left,
            currentWindowBounds.Top,
            Math.Max(targetWindowWidth, 200),
            Math.Max(targetWindowHeight, 200),
            SwpNoZOrder | SwpNoActivate);
    }

    internal static bool RequestClose(IntPtr windowHandle)
    {
        return PostMessage(windowHandle, WmClose, IntPtr.Zero, IntPtr.Zero);
    }

    private delegate bool EnumWindowsCallback(IntPtr windowHandle, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal int Width => Right - Left;
        internal int Height => Bottom - Top;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out int processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr windowHandle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr windowHandle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr windowHandle, StringBuilder text, int maximumCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr windowHandle, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(
        IntPtr windowHandle,
        uint message,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int value,
        int valueSize);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr iconHandle);
}
