using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BrowserGate.Services;

namespace BrowserGate.Views;

public partial class SettingsWindow : Window
{
    private int _relockMinutes = 5;

    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
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
        EdgeStatusTxt.Text = edge ? "Locked" : "Open to anyone";
        ChromeStatusTxt.Text = chrome ? "Locked" : "Open to anyone";

        _relockMinutes = Math.Clamp(cfg.AutoRelockMinutes, 0, 1440);
        RelockVal.Text = _relockMinutes.ToString();

        // Theme segmented control
        var theme = string.IsNullOrWhiteSpace(cfg.Theme) ? ThemeManager.Current : cfg.Theme;
        ThemeDark.IsChecked  = theme.Equals(ThemeManager.Dark,  StringComparison.OrdinalIgnoreCase);
        ThemeLight.IsChecked = theme.Equals(ThemeManager.Light, StringComparison.OrdinalIgnoreCase);

        // Status hero
        bool guarded = edge || chrome;
        if (guarded)
        {
            string which = (edge, chrome) switch
            {
                (true, true)  => "Edge and Chrome are locked.",
                (true, false) => "Microsoft Edge is locked.",
                _             => "Google Chrome is locked."
            };
            StatusTxt.Text = "Protected";
            StatusTxt.Foreground = (Brush)FindResource("Safe");
            StatusSub.Text = $"{which} Auto re-lock after {_relockMinutes} min.";
            HeroCard.Background = (Brush)FindResource("SafeBg");
            HeroCard.BorderBrush = (Brush)FindResource("SafeBorder");
            HeroIcon.Background = (Brush)FindResource("Safe");
            HeroIconPath.Stroke = (Brush)FindResource("SafeInk");
            HeroDot.Fill = (Brush)FindResource("Safe");
            HeroIconPath.Data = Geometry.Parse("M 12,3 L 21,7 V 12 C 21,17 17,21 12,22 C 7,21 3,17 3,12 V 7 Z M 8.5,12 L 11,14.5 L 16,9");
        }
        else
        {
            StatusTxt.Text = "Not protected";
            StatusTxt.Foreground = (Brush)FindResource("Danger");
            StatusSub.Text = "No browsers are being locked. Anyone can open them.";
            HeroCard.Background = (Brush)FindResource("DangerBg");
            HeroCard.BorderBrush = (Brush)FindResource("DangerBorder");
            HeroIcon.Background = (Brush)FindResource("Danger");
            HeroIconPath.Stroke = (Brush)FindResource("DangerInk");
            HeroDot.Fill = (Brush)FindResource("Danger");
            HeroIconPath.Data = Geometry.Parse("M 12,3 L 21,7 V 12 C 21,17 17,21 12,22 C 7,21 3,17 3,12 V 7 Z M 12,8 V 13 M 12,16 V 16.5");
        }
    }

    private void ApplyLocks_Click(object sender, RoutedEventArgs e)
    {
        bool wantEdge = LockEdgeChk.IsChecked == true;
        bool wantChrome = LockChromeChk.IsChecked == true;
        bool isEdge = IFEORegistrar.IsInstalled(IFEORegistrar.EdgeExe);
        bool isChrome = IFEORegistrar.IsInstalled(IFEORegistrar.ChromeExe);

        if (wantEdge == isEdge && wantChrome == isChrome) { Refresh(); return; }

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

        if (wantEdge || wantChrome) ElevatedHelper.InstallTask(exe);
        else ElevatedHelper.UninstallTask();

        if (ConfigStore.Exists())
        {
            var c = ConfigStore.Load();
            c.LockEdge = wantEdge; c.LockChrome = wantChrome;
            c.IfeoInstalled = wantEdge || wantChrome;
            ConfigStore.Save(c);
        }
    }

    private void RelockDown_Click(object sender, RoutedEventArgs e) => StepRelock(-1);
    private void RelockUp_Click  (object sender, RoutedEventArgs e) => StepRelock(+1);
    private void StepRelock(int delta)
    {
        _relockMinutes = Math.Clamp(_relockMinutes + delta, 0, 1440);
        RelockVal.Text = _relockMinutes.ToString();
        if (ConfigStore.Exists())
        {
            var cfg = ConfigStore.Load();
            cfg.AutoRelockMinutes = _relockMinutes;
            ConfigStore.Save(cfg);
        }
        Refresh();
        MsgTxt.Text = _relockMinutes == 0
            ? "Auto re-lock disabled."
            : $"Auto re-lock set to {_relockMinutes} minute{(_relockMinutes == 1 ? "" : "s")}.";
    }

    private void ThemeDark_Click (object sender, RoutedEventArgs e) => SetTheme(ThemeManager.Dark);
    private void ThemeLight_Click(object sender, RoutedEventArgs e) => SetTheme(ThemeManager.Light);
    private void SetTheme(string theme)
    {
        ThemeManager.Apply(theme);
        if (ConfigStore.Exists())
        {
            var cfg = ConfigStore.Load();
            cfg.Theme = theme;
            ConfigStore.Save(cfg);
        }
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
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserGate");
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(this,
            "Remove BrowserGate completely?\n\n· Unlocks Edge and Chrome\n· Deletes stored config\n· Closes the app",
            "Uninstall BrowserGate", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (r != MessageBoxResult.Yes) return;

        if (!IFEORegistrar.IsElevated())
        {
            IFEORegistrar.RelaunchElevated("--uninstall");
            Close();
            return;
        }

        Uninstaller.Run();
        MessageBox.Show(this, "BrowserGate has been removed. You can now delete the BrowserGate.exe file.", "Done");
        Application.Current.Shutdown();
    }
}
