using System.Diagnostics;
using System.IO;

namespace BrowserGate.Services;

/// <summary>
/// IFEO launches BrowserGate unelevated. The registry-toggle step needs admin.
/// At install time we create a Windows Scheduled Task running as the current
/// user with HIGHEST privileges. The unprivileged lock prompt writes a launch
/// request file, then triggers the task — which spawns an elevated
/// BrowserGate.exe --elevated-launch that performs the IFEO toggle + browser
/// start. Works without UAC prompt for users in the Administrators group.
///
/// IMPORTANT: each request gets a unique file name. Chrome re-execs itself
/// 2–3 times in the same instant when opening a new tab — overwriting a
/// single shared launch.req loses requests and tabs hang. The elevated
/// pass drains ALL pending request files in one go.
/// </summary>
public static class ElevatedHelper
{
    public const string TaskName = "BrowserGate Helper";

    private static string RequestDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BrowserGate");

    private static string PendingDir() => Path.Combine(RequestDir(), "pending");

    public static void InstallTask(string exePath)
    {
        var user = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var args =
            $"/create /tn \"{TaskName}\" " +
            $"/tr \"\\\"{exePath}\\\" --elevated-launch\" " +
            $"/sc once /st 00:00 /sd 01/01/2099 " +
            $"/ru \"{user}\" /rl HIGHEST /f";
        RunSchtasks(args);
    }

    public static void UninstallTask() => RunSchtasks($"/delete /tn \"{TaskName}\" /f");

    public static bool TaskExists()
    {
        var psi = new ProcessStartInfo("schtasks.exe", $"/query /tn \"{TaskName}\"")
        { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        try { using var p = Process.Start(psi); p?.WaitForExit(5000); return p?.ExitCode == 0; }
        catch { return false; }
    }

    public static void RequestLaunch(string browserExeKey, string browserExePath, string[] forwardedArgs)
    {
        Directory.CreateDirectory(PendingDir());
        // Unique filename per request — no shared file, no clobbering when
        // Chrome fires 3 concurrent re-execs in the same instant.
        var fileName = $"req_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.txt";
        var fullPath = Path.Combine(PendingDir(), fileName);
        var lines = new List<string> { browserExeKey, browserExePath };
        lines.AddRange(forwardedArgs);
        File.WriteAllLines(fullPath, lines);
        Log($"Queued request: {fileName} key={browserExeKey} args={forwardedArgs.Length}");
        RunSchtasks($"/run /tn \"{TaskName}\"");
    }

    /// <summary>
    /// Drain every queued request, oldest first. The elevated process acts
    /// on all of them inside ONE IFEO-bypass window.
    /// </summary>
    public static List<(string exeKey, string exePath, string[] args)> DrainAllRequests()
    {
        var results = new List<(string, string, string[])>();
        var dir = PendingDir();
        if (!Directory.Exists(dir)) return results;

        var files = Directory.GetFiles(dir, "req_*.txt").OrderBy(f => f).ToArray();
        foreach (var f in files)
        {
            try
            {
                var lines = File.ReadAllLines(f);
                File.Delete(f);
                if (lines.Length < 2) continue;
                results.Add((lines[0], lines[1], lines.Skip(2).ToArray()));
            }
            catch (Exception ex) { Log($"Drain skip {Path.GetFileName(f)}: {ex.Message}"); }
        }
        return results;
    }

    public static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(RequestDir());
            File.AppendAllText(Path.Combine(RequestDir(), "debug.log"),
                $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
        catch { }
    }

    private static void RunSchtasks(string args)
    {
        var psi = new ProcessStartInfo("schtasks.exe", args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        p?.WaitForExit(10000);
    }
}
