using System.Diagnostics;

namespace BrowserGate.Services;

/// <summary>
/// After a successful unlock, waits until all instances of the browser process
/// have exited, then waits N minutes, then force-kills any lingering instances
/// so the next launch goes back through the lock.
/// </summary>
public static class AutoRelock
{
    public static void Schedule(string processName, int minutes)
    {
        if (minutes <= 0) return;
        var name = processName;
        Task.Run(async () =>
        {
            // wait until no instances running, then wait minutes more
            while (Process.GetProcessesByName(name).Length > 0)
                await Task.Delay(TimeSpan.FromSeconds(15));

            await Task.Delay(TimeSpan.FromMinutes(minutes));

            // kill any that may have come back (lingering helpers)
            EdgeLauncher.KillAll(name);
            // clear the session so the next launch goes back through the lock
            var exeKey = name.Equals("chrome", StringComparison.OrdinalIgnoreCase)
                ? IFEORegistrar.ChromeExe : IFEORegistrar.EdgeExe;
            UnlockSession.Clear(exeKey);
        });
    }
}
