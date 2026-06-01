using System.Windows;
using System.Windows.Input;
using BrowserGate.Services;

namespace BrowserGate.Views;

public partial class PasswordPromptWindow : Window
{
    private readonly string[] _forwardedArgs;
    private readonly string _browserExeKey;
    private int _attempts;

    public PasswordPromptWindow(string browserExeKey, string[] forwardedArgs)
    {
        InitializeComponent();
        _forwardedArgs = forwardedArgs;
        _browserExeKey = browserExeKey;
        string display = browserExeKey.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase)
            ? "Google Chrome" : "Microsoft Edge";
        TitleTxt.Text = $"{display} is locked";
        HeaderTxt.Text = $"BrowserGate - {display}";
        Loaded += (_, _) => PwdBox.Focus();
    }

    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void PwdBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Unlock_Click(sender, e); }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigStore.Load();
        if (!ConfigStore.Verify(PwdBox.Password, cfg))
        {
            _attempts++;
            ErrorTxt.Text = $"Wrong password. Attempt {_attempts} of 5.";
            PwdBox.Clear();
            if (_attempts >= 5) { MessageBox.Show(this, "Too many attempts.", "BrowserGate"); Close(); }
            return;
        }

        try
        {
            ElevatedHelper.RequestLaunch(_browserExeKey, _forwardedArgs);
            string processName = _browserExeKey.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase)
                ? "chrome" : "msedge";
            AutoRelock.Schedule(processName, cfg.AutoRelockMinutes);
        }
        catch (Exception ex) { MessageBox.Show(this, "Launch failed: " + ex.Message, "BrowserGate"); }
        Close();
    }

    private void Forgot_Click(object sender, RoutedEventArgs e)
    {
        var reset = new ResetWindow { Owner = this };
        if (reset.ShowDialog() == true)
            MessageBox.Show(this, "Password updated. Enter the new password.", "BrowserGate");
    }
}
