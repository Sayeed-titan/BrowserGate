using System.Windows;
using BrowserGate.Services;
using BrowserGate.Views;

namespace BrowserGate;

public partial class App : Application
{
    private TrayApp? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ===== Elevated CLI flags =====
        for (int i = 0; i < e.Args.Length; i++)
        {
            var a = e.Args[i];
            if (a.Equals("--install", StringComparison.OrdinalIgnoreCase))
            {
                var cfg = ConfigStore.Exists() ? ConfigStore.Load() : new AppConfig { LockEdge = true };
                SettingsWindow.ApplyLockState(cfg.LockEdge, cfg.LockChrome);
                Shutdown(); return;
            }
            if (a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
            {
                Uninstaller.Run();
                Shutdown(); return;
            }
            if (a.Equals("--elevated-launch", StringComparison.OrdinalIgnoreCase))
            {
                var cfg = ConfigStore.Exists() ? ConfigStore.Load() : new AppConfig();
                var req = ElevatedHelper.ReadAndClearLaunchRequest();
                if (req.HasValue)
                {
                    var (exeKey, args) = req.Value;
                    var browserPath = exeKey.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase)
                        ? cfg.ChromePath : cfg.EdgePath;
                    try { EdgeLauncher.LaunchBrowser(exeKey, browserPath, args); } catch { }
                }
                Shutdown(); return;
            }
            if (a.Equals("--apply", StringComparison.OrdinalIgnoreCase))
            {
                bool wantEdge = false, wantChrome = false;
                for (int j = i + 1; j < e.Args.Length; j++)
                {
                    var s = e.Args[j];
                    if (s.StartsWith("edge=", StringComparison.OrdinalIgnoreCase))
                        wantEdge = s.EndsWith("True", StringComparison.OrdinalIgnoreCase);
                    if (s.StartsWith("chrome=", StringComparison.OrdinalIgnoreCase))
                        wantChrome = s.EndsWith("True", StringComparison.OrdinalIgnoreCase);
                }
                SettingsWindow.ApplyLockState(wantEdge, wantChrome);
                Shutdown(); return;
            }
        }

        // ===== Lock mode (we were invoked as IFEO Debugger) =====
        string? browserExeKey = null;
        var forwarded = new List<string>();
        foreach (var a in e.Args)
        {
            if (a.IndexOf("msedge.exe", StringComparison.OrdinalIgnoreCase) >= 0)
            { browserExeKey = "msedge.exe"; continue; }
            if (a.IndexOf("chrome.exe", StringComparison.OrdinalIgnoreCase) >= 0)
            { browserExeKey = "chrome.exe"; continue; }
            forwarded.Add(a);
        }

        if (!ConfigStore.Exists())
        {
            var setup = new SetupWindow();
            setup.Closed += (_, _) => Shutdown();
            setup.Show();
            return;
        }

        if (browserExeKey != null)
        {
            var prompt = new PasswordPromptWindow(browserExeKey, forwarded.ToArray());
            prompt.Closed += (_, _) => Shutdown();
            prompt.Show();
            return;
        }

        // ===== Tray mode (user double-clicked BrowserGate.exe) =====
        _tray = new TrayApp();
        var win = new SettingsWindow();
        win.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
