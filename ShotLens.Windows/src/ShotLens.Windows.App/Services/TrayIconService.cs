using System.Drawing;
using System.Windows.Forms;

namespace ShotLens.Windows.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Action showWindow;
    private readonly Action startCapture;
    private readonly Action exit;
    private NotifyIcon? notifyIcon;

    public TrayIconService(Action showWindow, Action startCapture, Action exit)
    {
        this.showWindow = showWindow;
        this.startCapture = startCapture;
        this.exit = exit;
    }

    public void Start()
    {
        notifyIcon = new NotifyIcon
        {
            Text = "ShotLens",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        notifyIcon.DoubleClick += (_, _) => showWindow();
    }

    public void Dispose()
    {
        if (notifyIcon is null)
        {
            return;
        }

        notifyIcon.Visible = false;
        notifyIcon.Dispose();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => showWindow());
        menu.Items.Add("开始截图", null, (_, _) => startCapture());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出 ShotLens", null, (_, _) => exit());
        return menu;
    }
}
