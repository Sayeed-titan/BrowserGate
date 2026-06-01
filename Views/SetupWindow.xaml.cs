using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using EdgeLocker.Services;

namespace EdgeLocker.Views;

public partial class SetupWindow : Window
{
    public SetupWindow() { InitializeComponent(); }

    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenLink(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        e.Handled = true;
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
        if (app.Length < 12) { ErrorTxt.Text = "Enter your 16-char Gmail App Password."; return; }
        if (LockEdgeChk.IsChecked != true && LockChromeChk.IsChecked != true)
        { ErrorTxt.Text = "Pick at least one browser to lock."; return; }
        if (!int.TryParse(RelockBox.Text, out var minutes) || minutes < 0 || minutes > 1440)
        { ErrorTxt.Text = "Auto-relock must be between 0 and 1440 minutes."; return; }

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
            AutoRelockMinutes = minutes,
            IfeoInstalled = false
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
