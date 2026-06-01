using System.IO;

namespace BrowserGate.Services;

/// <summary>
/// A successful password entry "unlocks" the browser for its current session.
/// Subsequent IFEO triggers for the same browser (Chrome re-execs itself with
/// flags like --user-data-dir, --first-run, etc. that do not carry --type=)
/// pass through silently using this token. The token is cleared by AutoRelock
/// after the user closes the browser + N minutes elapse, or by the tray
/// "Lock browsers now" action.
/// </summary>
public static class UnlockSession
{
    private static string Dir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BrowserGate");

    private static string TokenPath(string browserExeKey) =>
        Path.Combine(Dir(), $"unlocked.{browserExeKey}");

    public static bool IsUnlocked(string browserExeKey)
    {
        if (!File.Exists(TokenPath(browserExeKey))) return false;
        // Stale-token guard: if no browser process is running, the previous
        // session ended (e.g. reboot, force-quit). Clear and require re-auth.
        var procName = browserExeKey.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase)
            ? "chrome" : "msedge";
        if (System.Diagnostics.Process.GetProcessesByName(procName).Length == 0)
        {
            Clear(browserExeKey);
            return false;
        }
        return true;
    }

    public static void Mark(string browserExeKey)
    {
        try
        {
            Directory.CreateDirectory(Dir());
            File.WriteAllText(TokenPath(browserExeKey), DateTime.UtcNow.ToString("o"));
        }
        catch { }
    }

    public static void Clear(string browserExeKey)
    {
        try { File.Delete(TokenPath(browserExeKey)); } catch { }
    }

    public static void ClearAll()
    {
        Clear(IFEORegistrar.EdgeExe);
        Clear(IFEORegistrar.ChromeExe);
    }
}
