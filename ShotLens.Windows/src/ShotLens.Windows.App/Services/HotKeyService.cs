using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ShotLens.Windows.App.Services;

public sealed class HotKeyService : IDisposable
{
    private const int HotKeyId = 0x5348;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const int WmHotKey = 0x0312;

    private readonly Window window;
    private readonly Action callback;
    private HwndSource? source;
    private ShortcutGesture shortcut;

    public HotKeyService(Window window, ShortcutGesture shortcut, Action callback)
    {
        this.window = window;
        this.shortcut = shortcut;
        this.callback = callback;
    }

    public void Register()
    {
        var helper = new WindowInteropHelper(window);
        source = HwndSource.FromHwnd(helper.Handle);
        source?.AddHook(WndProc);
        if (!RegisterHotKey(helper.Handle, HotKeyId, ModifierFlags(shortcut), VirtualKey(shortcut)))
        {
            throw new InvalidOperationException($"快捷键 {shortcut.DisplayText} 注册失败，可能已被其他应用占用。");
        }
    }

    public void Update(ShortcutGesture newShortcut)
    {
        var helper = new WindowInteropHelper(window);
        UnregisterHotKey(helper.Handle, HotKeyId);
        shortcut = newShortcut;
        if (!RegisterHotKey(helper.Handle, HotKeyId, ModifierFlags(shortcut), VirtualKey(shortcut)))
        {
            throw new InvalidOperationException($"快捷键 {shortcut.DisplayText} 注册失败，可能已被其他应用占用。");
        }
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

    private static uint ModifierFlags(ShortcutGesture gesture)
    {
        uint flags = 0;
        if (gesture.Control) flags |= ModControl;
        if (gesture.Alt) flags |= ModAlt;
        if (gesture.Shift) flags |= ModShift;
        if (gesture.Windows) flags |= ModWin;
        return flags;
    }

    private static uint VirtualKey(ShortcutGesture gesture) =>
        (uint)KeyInterop.VirtualKeyFromKey(gesture.ToWpfKey());
}
