using System.Windows;
using BrowserGate.Services;
using BrowserGate.Views;

namespace BrowserGate;

public partial class App : Application
{
    private TrayApp? _tray;
    private System.Threading.Mutex? _trayMutex;

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
                // Settle so concurrent requests landing in the same moment
                // (Chrome's new-tab burst fires 2-3 re-execs back-to-back)
                // end up in the same drain pass.
                System.Threading.Thread.Sleep(300);

                int rounds = 0;
                while (rounds < 4) // bounded loop catches late-arriving burst items
                {
                    var items = ElevatedHelper.DrainAllRequests();
                    if (items.Count == 0) break;
                    try
                    {
                        EdgeLauncher.LaunchBatch(items);
                        ElevatedHelper.Log($"LaunchBatch OK (round {rounds + 1}): {items.Count} item(s)");
                    }
                    catch (Exception ex) { ElevatedHelper.Log("LaunchBatch FAIL: " + ex.Message); }
                    rounds++;
                    System.Threading.Thread.Sleep(400); // wait for stragglers
                }
                if (rounds == 0) ElevatedHelper.Log("--elevated-launch: no pending requests");
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
                    // Refresh the grace window so an actively-used browser
                    // stays unlocked beyond the initial 60 s.
                    UnlockSession.Mark(browserExeKey);
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
        // Single-instance guard: if another tray is already running, surface
        // its Settings window and exit. Prevents the "10 tray icons" pile-up
        // when the user double-clicks the exe multiple times.
        _trayMutex = new System.Threading.Mutex(true, "Local\\BrowserGate_TraySingleton", out bool fresh);
        if (!fresh)
        {
            try
            {
                // Best-effort: tell the existing instance to open Settings via
                // its own DoubleClick handler — done by simulating a re-entry
                // for the user, just exit silently here.
            }
            catch { }
            Shutdown();
            return;
        }

        _tray = new TrayApp();
        var win = new SettingsWindow();
        win.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        try { _trayMutex?.ReleaseMutex(); } catch { }
        _trayMutex?.Dispose();
        base.OnExit(e);
    }
}
