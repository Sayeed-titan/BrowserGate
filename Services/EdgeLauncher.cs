using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace BrowserGate.Services;

public static class EdgeLauncher
{
    private static string IfeoPath(string exe) =>
        $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exe}";

    public static string FindEdgePath()
    {
        string[] c = {
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
        };
        foreach (var p in c) if (File.Exists(p)) return p;
        return c[0];
    }

    public static string FindChromePath()
    {
        string[] c = {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Google\Chrome\Application\chrome.exe")
        };
        foreach (var p in c) if (File.Exists(p)) return p;
        return "";
    }

    /// <summary>
    /// Temporarily removes the IFEO Debugger for the given exe, launches it, restores IFEO.
    /// Uses a global mutex to avoid recursion. Caller must be elevated.
    /// </summary>
    public static void LaunchBrowser(string exeKey, string browserPath, string[] forwardedArgs)
        => LaunchBatch(new[] { (exeKey, browserPath, forwardedArgs) });

    /// <summary>
    /// Run several launches inside ONE IFEO-bypass window. This is critical
    /// when Chrome opens a new tab: it fires 2–3 re-execs in the same instant
    /// and serialising them through the 1.5 s mutex would queue tabs for
    /// seconds and break Chrome's single-instance protocol.
    /// </summary>
    public static void LaunchBatch(IReadOnlyList<(string exeKey, string browserPath, string[] forwardedArgs)> items)
    {
        if (items.Count == 0) return;

        using var mtx = new System.Threading.Mutex(false, "Global\\BrowserGate_LaunchMutex");
        bool taken = false;
        try { taken = mtx.WaitOne(TimeSpan.FromSeconds(5)); }
        catch (AbandonedMutexException) { taken = true; }

        // Snapshot + remove Debugger for every distinct exeKey we're about to launch.
        var distinctKeys = items.Select(x => x.exeKey).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var saved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in distinctKeys)
        {
            using var k = Registry.LocalMachine.OpenSubKey(IfeoPath(key), true);
            var dbg = k?.GetValue("Debugger") as string;
            saved[key] = dbg;
            if (k != null && dbg != null) k.DeleteValue("Debugger", false);
        }

        try
        {
            foreach (var (_, browserPath, args) in items)
            {
                try
                {
                    var psi = new ProcessStartInfo { FileName = browserPath, UseShellExecute = false };
                    foreach (var a in args) psi.ArgumentList.Add(a);
                    Process.Start(psi);
                }
                catch { /* per-item failures shouldn't abort the batch */ }
            }
            // Give Chrome a moment to spawn its child processes before we put
            // the IFEO Debugger back. 1.5 s covers Chrome's startup spawn burst.
            System.Threading.Thread.Sleep(1500);
        }
        finally
        {
            foreach (var key in distinctKeys)
            {
                if (saved.TryGetValue(key, out var dbg) && dbg != null)
                {
                    using var k = Registry.LocalMachine.CreateSubKey(IfeoPath(key), true);
                    k.SetValue("Debugger", dbg);
                }
            }
            if (taken) mtx.ReleaseMutex();
        }
    }

    public static void KillAll(string processName)
    {
        foreach (var p in Process.GetProcessesByName(processName))
        {
            try { p.Kill(true); } catch { }
        }
    }
}
