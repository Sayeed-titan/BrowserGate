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

        // Apply the user's saved theme before any window is created.
        try
        {
            var initial = ConfigStore.Exists() ? ConfigStore.Load().Theme : ThemeManager.Dark;
            ThemeManager.Apply(initial);
        }
        catch { ThemeManager.Apply(ThemeManager.Dark); }

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
                ElevatedHelper.Log("--elevated-launch started");
                var req = ElevatedHelper.ReadAndClearLaunchRequest();
                if (req.HasValue)
                {
                    var (exeKey, exePath, args) = req.Value;
                    try
                    {
                        EdgeLauncher.LaunchBrowser(exeKey, exePath, args);
                        ElevatedHelper.Log($"LaunchBrowser OK: {exePath}");
                    }
                    catch (Exception ex) { ElevatedHelper.Log("LaunchBrowser FAIL: " + ex.Message); }
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
        // IFEO passes: <original-exe-full-path> <original-args...>
        string? browserExeKey = null;
        string browserExePath = "";
        var forwarded = new List<string>();
        foreach (var a in e.Args)
        {
            if (browserExeKey == null && a.IndexOf("msedge.exe", StringComparison.OrdinalIgnoreCase) >= 0)
            { browserExeKey = "msedge.exe"; browserExePath = a; continue; }
            if (browserExeKey == null && a.IndexOf("chrome.exe", StringComparison.OrdinalIgnoreCase) >= 0)
            { browserExeKey = "chrome.exe"; browserExePath = a; continue; }
            forwarded.Add(a);
        }
        ElevatedHelper.Log($"Lock-mode args: key={browserExeKey} path={browserExePath} extra={forwarded.Count}");

        if (!ConfigStore.Exists())
        {
            var setup = new SetupWindow();
            setup.Closed += (_, _) => Shutdown();
            setup.Show();
            return;
        }

        if (browserExeKey != null)
        {
            // If the browser was already unlocked this session, pass through
            // silently. Chrome/Edge re-exec themselves with switches that don't
            // carry --type=, so IFEO catches them; the session token tells us
            // the user already authenticated.
            if (UnlockSession.IsUnlocked(browserExeKey))
            {
                try
                {
                    ElevatedHelper.Log($"Pass-through (session unlocked): {browserExeKey} args={forwarded.Count}");
                    ElevatedHelper.RequestLaunch(browserExeKey, browserExePath, forwarded.ToArray());
                }
                catch (Exception ex) { ElevatedHelper.Log("Pass-through FAIL: " + ex.Message); }
                Shutdown();
                return;
            }

            var prompt = new PasswordPromptWindow(browserExeKey, browserExePath, forwarded.ToArray());
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
