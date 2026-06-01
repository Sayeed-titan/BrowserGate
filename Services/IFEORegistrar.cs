using System.Diagnostics;
using Microsoft.Win32;

namespace BrowserGate.Services;

public static class IFEORegistrar
{
    public const string EdgeExe = "msedge.exe";
    public const string ChromeExe = "chrome.exe";

    private static string Path(string exe) =>
        $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exe}";

    public static bool IsInstalled(string exe)
    {
        using var k = Registry.LocalMachine.OpenSubKey(Path(exe));
        return k?.GetValue("Debugger") is string s && !string.IsNullOrWhiteSpace(s);
    }

    public static void Install(string exe, string lockerExePath)
    {
        using var k = Registry.LocalMachine.CreateSubKey(Path(exe), true);
        k.SetValue("Debugger", $"\"{lockerExePath}\"");
        // UseFilter + subkey "0" matching "--type=" bypasses the lock for
        // Chromium child processes (renderer, gpu, utility, etc.) so the
        // user is only prompted on the *initial* user-clicked launch.
        k.SetValue("UseFilter", 1, RegistryValueKind.DWord);
        using var sub = k.CreateSubKey("0", true);
        sub.SetValue("FilterCommandLine", "--type=");
        // No Debugger value here = child processes launch normally.
    }

    public static void Uninstall(string exe)
    {
        using var k = Registry.LocalMachine.OpenSubKey(Path(exe), true);
        if (k == null) return;
        if (k.GetValue("Debugger") != null) k.DeleteValue("Debugger", false);
        if (k.GetValue("UseFilter") != null) k.DeleteValue("UseFilter", false);
        try { k.DeleteSubKeyTree("0", false); } catch { }
    }

    public static bool IsElevated()
    {
        using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
        return new System.Security.Principal.WindowsPrincipal(id)
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    public static bool RelaunchElevated(string args)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? "",
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas"
            });
            return true;
        }
        catch { return false; }
    }
}
