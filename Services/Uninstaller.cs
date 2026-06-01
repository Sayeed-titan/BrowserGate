using System.IO;

namespace EdgeLocker.Services;

public static class Uninstaller
{
    public static void Run()
    {
        IFEORegistrar.Uninstall(IFEORegistrar.EdgeExe);
        IFEORegistrar.Uninstall(IFEORegistrar.ChromeExe);

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EdgeLocker");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch { }
    }
}
