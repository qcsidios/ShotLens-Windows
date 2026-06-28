using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ShotLens.Windows.App.Services;

public sealed class HotKeyService : IDisposable
{
    private const int HotKeyId = 0x5348;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkT = 0x54;
    private const int WmHotKey = 0x0312;

    private readonly Window window;
    private readonly Action callback;
    private HwndSource? source;

    public HotKeyService(Window window, Action callback)
    {
        this.window = window;
        this.callback = callback;
    }

    public void Register()
    {
        var helper = new WindowInteropHelper(window);
        source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook(WndProc);
        RegisterHotKey(helper.Handle, HotKeyId, ModControl | ModAlt, VkT);
    }

    public void Dispose()
    {
        var helper = new WindowInteropHelper(window);
        UnregisterHotKey(helper.Handle, HotKeyId);
        source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == HotKeyId)
        {
            callback();
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
