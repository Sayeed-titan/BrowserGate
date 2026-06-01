using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using BrowserGate.Views;
using WF = System.Windows.Forms;

namespace BrowserGate.Services;

public sealed class TrayApp : IDisposable
{
    private readonly WF.NotifyIcon _ni;
    private readonly WF.ContextMenuStrip _menu;
    private readonly WF.Timer _timer;

    public TrayApp()
    {
        _menu = new WF.ContextMenuStrip { ShowImageMargin = false };
        var statusItem = new WF.ToolStripMenuItem { Enabled = false };
        var settingsItem = new WF.ToolStripMenuItem("Open Settings");
        var lockNowItem  = new WF.ToolStripMenuItem("Lock browsers now");
        var quitItem     = new WF.ToolStripMenuItem("Quit");

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
            Icon = LoadAppIcon() ?? BuildFallbackIcon(),
            Visible = true,
            Text = "BrowserGate",
            ContextMenuStrip = _menu
        };
        _ni.DoubleClick += (_, _) => OpenSettings();

        UpdateStatus(statusItem);
        _timer = new WF.Timer { Interval = 5000 };
        _timer.Tick += (_, _) => UpdateStatus(statusItem);
        _timer.Start();

        // Belt-and-braces: ensure the tray icon disappears even on unexpected shutdown.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => SafeHide();
        Application.Current.Exit += (_, _) => SafeHide();
    }

    private void SafeHide()
    {
        try { _ni.Visible = false; _ni.Dispose(); } catch { }
    }

    private static void UpdateStatus(WF.ToolStripMenuItem item)
    {
        bool edge = IFEORegistrar.IsInstalled(IFEORegistrar.EdgeExe);
        bool chrome = IFEORegistrar.IsInstalled(IFEORegistrar.ChromeExe);
        item.Text = (edge, chrome) switch
        {
            (true, true)  => "Edge + Chrome locked",
            (true, false) => "Edge locked",
            (false, true) => "Chrome locked",
            _             => "Browsers not locked"
        };
    }

    private static void OpenSettings()
    {
        var existing = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing != null) { existing.Activate(); return; }
        new SettingsWindow().Show();
    }

    /// <summary>Load the embedded BrowserGate.ico resource at the best size for the tray.</summary>
    private static Icon? LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/BrowserGate.ico", UriKind.Absolute);
            using var s = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (s == null) return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            ms.Position = 0;
            return new Icon(ms, WF.SystemInformation.SmallIconSize);
        }
        catch { return null; }
    }

    private static Icon BuildFallbackIcon()
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var bg = new SolidBrush(Color.FromArgb(10, 125, 154));
            g.FillEllipse(bg, 2, 2, 28, 28);
            using var pen = new Pen(Color.White, 2.5f);
            g.DrawArc(pen, 11, 9, 10, 10, 180, 180);
            using var body = new SolidBrush(Color.White);
            g.FillRectangle(body, 10, 16, 12, 9);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose() => SafeHide();
}
