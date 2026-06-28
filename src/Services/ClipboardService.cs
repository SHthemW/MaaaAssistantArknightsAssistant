using System.Runtime.InteropServices;

namespace Game_Daily_Routine_Launcher;

public static class ClipboardService
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint GMEM_ZEROINIT = 0x0040;

    public static Task<bool> TrySetTextAsync(string text, int retryCount = 12, int delayMs = 50)
        => Task.Run(() => TrySetText(text, retryCount, delayMs));

    private static bool TrySetText(string text, int retryCount, int delayMs)
    {
        for (var attempt = 0; attempt < retryCount; attempt++)
        {
            if (TrySetTextOnce(text))
                return true;

            Thread.Sleep(delayMs);
        }

        return false;
    }

    private static bool TrySetTextOnce(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
            return false;

        try
        {
            if (!EmptyClipboard())
                return false;

            var bytes = (text.Length + 1) * sizeof(char);
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, (UIntPtr)bytes);
            if (hGlobal == IntPtr.Zero)
                return false;

            var target = GlobalLock(hGlobal);
            if (target == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                return false;
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                return false;
            }

            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
