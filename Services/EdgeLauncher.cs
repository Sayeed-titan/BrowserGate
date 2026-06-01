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
    {
        using var mtx = new System.Threading.Mutex(false, "Global\\BrowserGate_LaunchMutex");
        bool taken = false;
        try { taken = mtx.WaitOne(TimeSpan.FromSeconds(5)); }
        catch (AbandonedMutexException) { taken = true; }

        try
        {
            string? saved = null;
            using (var k = Registry.LocalMachine.OpenSubKey(IfeoPath(exeKey), true))
            {
                saved = k?.GetValue("Debugger") as string;
                if (k != null && saved != null) k.DeleteValue("Debugger", false);
            }

            try
            {
                var psi = new ProcessStartInfo { FileName = browserPath, UseShellExecute = false };
                foreach (var a in forwardedArgs) psi.ArgumentList.Add(a);
                Process.Start(psi);
                System.Threading.Thread.Sleep(1500);
            }
            finally
            {
                if (saved != null)
                {
                    using var k = Registry.LocalMachine.CreateSubKey(IfeoPath(exeKey), true);
                    k.SetValue("Debugger", saved);
                }
            }
        }
        finally
        {
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
