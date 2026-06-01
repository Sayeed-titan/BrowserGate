using System.Drawing;
using System.Windows;
using BrowserGate.Views;
using WF = System.Windows.Forms;

namespace BrowserGate.Services;

public sealed class TrayApp : IDisposable
{
    private readonly WF.NotifyIcon _ni;
    private readonly WF.ContextMenuStrip _menu;

    public TrayApp()
    {
        _menu = new WF.ContextMenuStrip { ShowImageMargin = false };
        var statusItem = new WF.ToolStripMenuItem { Enabled = false };
        var settingsItem = new WF.ToolStripMenuItem("Open Settings");
        var lockNowItem = new WF.ToolStripMenuItem("Lock browsers now");
        var quitItem = new WF.ToolStripMenuItem("Quit");

        settingsItem.Click += (_, _) => OpenSettings();
        lockNowItem.Click += (_, _) =>
        {
            EdgeLauncher.KillAll("msedge");
            EdgeLauncher.KillAll("chrome");
            UnlockSession.ClearAll();
        };
        quitItem.Click += (_, _) =>
        {
            _ni.Visible = false;
            Application.Current.Shutdown();
        };

        _menu.Items.Add(statusItem);
        _menu.Items.Add(new WF.ToolStripSeparator());
        _menu.Items.Add(settingsItem);
        _menu.Items.Add(lockNowItem);
        _menu.Items.Add(new WF.ToolStripSeparator());
        _menu.Items.Add(quitItem);

        _ni = new WF.NotifyIcon
        {
            Icon = BuildIcon(),
            Visible = true,
            Text = "BrowserGate",
            ContextMenuStrip = _menu
        };
        _ni.DoubleClick += (_, _) => OpenSettings();

        UpdateStatus(statusItem);
        var timer = new WF.Timer { Interval = 5000 };
        timer.Tick += (_, _) => UpdateStatus(statusItem);
        timer.Start();
    }

    private static void UpdateStatus(WF.ToolStripMenuItem item)
    {
        bool edge = IFEORegistrar.IsInstalled(IFEORegistrar.EdgeExe);
        bool chrome = IFEORegistrar.IsInstalled(IFEORegistrar.ChromeExe);
        item.Text = (edge, chrome) switch
        {
            (true, true)  => "ðŸ”’ Edge + Chrome locked",
            (true, false) => "ðŸ”’ Edge locked",
            (false, true) => "ðŸ”’ Chrome locked",
            _             => "ðŸ”“ Browsers not locked"
        };
    }

    private static void OpenSettings()
    {
        var existing = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing != null) { existing.Activate(); return; }
        new SettingsWindow().Show();
    }

    private static Icon BuildIcon()
    {
        // Generate a simple lock-shaped icon at runtime so we have no asset dependency
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var bg = new SolidBrush(Color.FromArgb(59, 130, 246));
            g.FillEllipse(bg, 2, 2, 28, 28);
            using var pen = new Pen(Color.White, 2.5f);
            g.DrawArc(pen, 11, 9, 10, 10, 180, 180);
            using var body = new SolidBrush(Color.White);
            g.FillRectangle(body, 10, 16, 12, 9);
        }
        var hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        _ni.Visible = false;
        _ni.Dispose();
        _menu.Dispose();
    }
}
