using System.IO;

namespace BrowserGate.Services;

public static class Uninstaller
{
    public static void Run()
    {
        IFEORegistrar.Uninstall(IFEORegistrar.EdgeExe);
        IFEORegistrar.Uninstall(IFEORegistrar.ChromeExe);

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserGate");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch { }
    }
}
