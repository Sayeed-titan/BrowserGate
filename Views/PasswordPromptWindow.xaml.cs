using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using BrowserGate.Services;

namespace BrowserGate.Views;

public partial class PasswordPromptWindow : Window
{
    private readonly string[] _forwardedArgs;
    private readonly string _browserExeKey;
    private readonly string _browserExePath;
    private int _attempts;
    private const int MaxAttempts = 5;

    public PasswordPromptWindow(string browserExeKey, string browserExePath, string[] forwardedArgs)
    {
        InitializeComponent();
        _forwardedArgs = forwardedArgs;
        _browserExeKey = browserExeKey;
        _browserExePath = browserExePath;
        string display = browserExeKey.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase)
            ? "Google Chrome" : "Microsoft Edge";
        TitleTxt.Text = $"{display} is locked";
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
            int left = Math.Max(0, MaxAttempts - _attempts);
            ErrorTxt.Text = left == 1
                ? "Incorrect password · 1 attempt left"
                : $"Incorrect password · {left} attempts left";
            ErrorTxt.Visibility = Visibility.Visible;
            PwdBox.Clear();
            Shake();
            if (_attempts >= MaxAttempts) { MessageBox.Show(this, "Too many attempts.", "BrowserGate"); Close(); }
            return;
        }

        try
        {
            UnlockSession.Mark(_browserExeKey);
            ElevatedHelper.RequestLaunch(_browserExeKey, _browserExePath, _forwardedArgs);
            string processName = _browserExeKey.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase)
                ? "chrome" : "msedge";
            AutoRelock.Schedule(processName, cfg.AutoRelockMinutes);
        }
        catch (Exception ex) { MessageBox.Show(this, "Launch failed: " + ex.Message, "BrowserGate"); }
        Close();
    }

    private void Shake()
    {
        var anim = new DoubleAnimationUsingKeyFrames();
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(0,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame( 8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(-6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame( 6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(0,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
        ShakeTx.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);
    }

    private void Forgot_Click(object sender, RoutedEventArgs e)
    {
        var reset = new ResetWindow { Owner = this };
        if (reset.ShowDialog() == true)
            MessageBox.Show(this, "Password updated. Enter the new password.", "BrowserGate");
    }
}
