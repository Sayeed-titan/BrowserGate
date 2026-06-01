using System.Diagnostics;
using System.IO;

namespace BrowserGate.Services;

/// <summary>
/// IFEO launches BrowserGate unelevated. The registry-toggle step needs admin.
/// Solution: at install time we create a Windows Scheduled Task running as the
/// current user with HIGHEST privileges. The unprivileged lock prompt writes
/// a launch request, then triggers the task — which spawns an elevated
/// BrowserGate.exe --elevated-launch that performs the IFEO toggle + browser start.
/// Works without UAC prompt for users in the Administrators group.
/// </summary>
public static class ElevatedHelper
{
    public const string TaskName = "BrowserGate Helper";

    private static string RequestDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BrowserGate");

    public static string LaunchRequestPath() => Path.Combine(RequestDir(), "launch.req");

    public static void InstallTask(string exePath)
    {
        var user = $"{Environment.UserDomainName}\\{Environment.UserName}";
        // /sc once with a far-future date = no scheduled trigger; we always use /run
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
        Directory.CreateDirectory(RequestDir());
        var lines = new List<string> { browserExeKey, browserExePath };
        lines.AddRange(forwardedArgs);
        File.WriteAllLines(LaunchRequestPath(), lines);
        Log($"RequestLaunch: key={browserExeKey} path={browserExePath} args={forwardedArgs.Length}");
        RunSchtasks($"/run /tn \"{TaskName}\"");
    }

    public static (string exeKey, string exePath, string[] args)? ReadAndClearLaunchRequest()
    {
        var path = LaunchRequestPath();
        if (!File.Exists(path)) { Log("ReadLaunchRequest: file not found"); return null; }
        var lines = File.ReadAllLines(path);
        try { File.Delete(path); } catch { }
        if (lines.Length < 2) { Log("ReadLaunchRequest: malformed"); return null; }
        return (lines[0], lines[1], lines.Skip(2).ToArray());
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
