using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EdgeLocker.Services;

namespace EdgeLocker.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Refresh();
    }

    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Refresh()
    {
        var cfg = ConfigStore.Exists() ? ConfigStore.Load() : new AppConfig();
        bool edge = IFEORegistrar.IsInstalled(IFEORegistrar.EdgeExe);
        bool chrome = IFEORegistrar.IsInstalled(IFEORegistrar.ChromeExe);

        LockEdgeChk.IsChecked = edge;
        LockChromeChk.IsChecked = chrome;
        RelockBox.Text = cfg.AutoRelockMinutes.ToString();

        if (edge || chrome)
        {
            StatusTxt.Text = "Protected";
            StatusSub.Text = (edge, chrome) switch
            {
                (true, true) => "Edge and Chrome both require your password.",
                (true, false) => "Edge requires your password.",
                _ => "Chrome requires your password."
            };
            StatusIcon.Background = (Brush)FindResource("Success");
        }
        else
        {
            StatusTxt.Text = "Unprotected";
            StatusSub.Text = "Browsers will open without a password.";
            StatusIcon.Background = (Brush)FindResource("Danger");
        }
    }

    private void ApplyLocks_Click(object sender, RoutedEventArgs e)
    {
        bool wantEdge = LockEdgeChk.IsChecked == true;
        bool wantChrome = LockChromeChk.IsChecked == true;
        bool isEdge = IFEORegistrar.IsInstalled(IFEORegistrar.EdgeExe);
        bool isChrome = IFEORegistrar.IsInstalled(IFEORegistrar.ChromeExe);

        if (wantEdge == isEdge && wantChrome == isChrome) return;

        if (!IFEORegistrar.IsElevated())
        {
            var args = $"--apply edge={wantEdge} chrome={wantChrome}";
            IFEORegistrar.RelaunchElevated(args);
            Close();
            return;
        }

        ApplyLockState(wantEdge, wantChrome);
        Refresh();
    }

    public static void ApplyLockState(bool wantEdge, bool wantChrome)
    {
        var exe = Environment.ProcessPath ?? "";
        if (wantEdge) IFEORegistrar.Install(IFEORegistrar.EdgeExe, exe);
        else IFEORegistrar.Uninstall(IFEORegistrar.EdgeExe);

        if (wantChrome) IFEORegistrar.Install(IFEORegistrar.ChromeExe, exe);
        else IFEORegistrar.Uninstall(IFEORegistrar.ChromeExe);

        if (ConfigStore.Exists())
        {
            var c = ConfigStore.Load();
            c.LockEdge = wantEdge; c.LockChrome = wantChrome;
            c.IfeoInstalled = wantEdge || wantChrome;
            ConfigStore.Save(c);
        }
    }

    private void SaveRelock_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(RelockBox.Text, out var m) || m < 0 || m > 1440)
        { MsgTxt.Text = "Enter a number between 0 and 1440."; return; }
        var cfg = ConfigStore.Load();
        cfg.AutoRelockMinutes = m;
        ConfigStore.Save(cfg);
        MsgTxt.Text = $"Auto-relock set to {m} minute(s).";
    }

    private void Change_Click(object sender, RoutedEventArgs e)
    {
        var reset = new ResetWindow { Owner = this };
        reset.ShowDialog();
    }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("msedge.exe") { UseShellExecute = true }); }
        catch (Exception ex) { MsgTxt.Text = ex.Message; }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EdgeLocker");
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(this,
            "Remove EdgeLocker completely?\n\n• Unlocks Edge and Chrome\n• Deletes stored config\n• Closes the app",
            "Uninstall EdgeLocker", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (r != MessageBoxResult.Yes) return;

        if (!IFEORegistrar.IsElevated())
        {
            IFEORegistrar.RelaunchElevated("--uninstall");
            Close();
            return;
        }

        Uninstaller.Run();
        MessageBox.Show(this, "EdgeLocker has been removed. You can now delete the EdgeLocker.exe file.", "Done");
        Application.Current.Shutdown();
    }
}
