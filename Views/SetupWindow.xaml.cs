using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using BrowserGate.Services;

namespace BrowserGate.Views;

public partial class SetupWindow : Window
{
    private int _relockMinutes = 5;

    public SetupWindow() { InitializeComponent(); }

    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenLink(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        e.Handled = true;
    }

    private void RelockDown_Click(object sender, RoutedEventArgs e) => StepRelock(-1);
    private void RelockUp_Click  (object sender, RoutedEventArgs e) => StepRelock(+1);
    private void StepRelock(int delta)
    {
        _relockMinutes = Math.Clamp(_relockMinutes + delta, 0, 1440);
        RelockVal.Text = _relockMinutes.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorTxt.Text = "";
        var p1 = PwdBox.Password;
        var p2 = PwdBox2.Password;
        var gmail = GmailBox.Text.Trim();
        var app = AppPwdBox.Password.Trim().Replace(" ", "");

        if (p1.Length < 6) { ErrorTxt.Text = "Password must be at least 6 characters."; return; }
        if (p1 != p2) { ErrorTxt.Text = "Passwords do not match."; return; }
        if (!gmail.Contains('@')) { ErrorTxt.Text = "Enter a valid Gmail address."; return; }
        if (app.Length < 12) { ErrorTxt.Text = "Enter your 16-character Gmail App Password."; return; }
        if (LockEdgeChk.IsChecked != true && LockChromeChk.IsChecked != true)
        { ErrorTxt.Text = "Pick at least one browser to lock."; return; }

        var (salt, hash) = ConfigStore.HashPassword(p1);
        var cfg = new AppConfig
        {
            Salt = salt,
            PasswordHash = hash,
            GmailAddress = gmail,
            GmailAppPassword = app,
            EdgePath = EdgeLauncher.FindEdgePath(),
            ChromePath = EdgeLauncher.FindChromePath(),
            LockEdge = LockEdgeChk.IsChecked == true,
            LockChrome = LockChromeChk.IsChecked == true,
            AutoRelockMinutes = _relockMinutes,
            IfeoInstalled = false,
            Theme = ThemeManager.Current
        };
        ConfigStore.Save(cfg);

        if (!IFEORegistrar.IsElevated())
        {
            IFEORegistrar.RelaunchElevated("--install");
            Close();
            return;
        }

        if (cfg.LockEdge) IFEORegistrar.Install(IFEORegistrar.EdgeExe, Environment.ProcessPath ?? "");
        if (cfg.LockChrome) IFEORegistrar.Install(IFEORegistrar.ChromeExe, Environment.ProcessPath ?? "");
        cfg.IfeoInstalled = true;
        ConfigStore.Save(cfg);
        Close();
    }
}
