using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BrowserGate.Services;

namespace BrowserGate.Views;

public partial class ResetWindow : Window
{
    private string? _otp;
    private DateTime _otpExpiry;
    private TextBox[] _cells = Array.Empty<TextBox>();

    public ResetWindow()
    {
        InitializeComponent();
        _cells = new[] { Otp0, Otp1, Otp2, Otp3, Otp4, Otp5 };
        for (int i = 0; i < _cells.Length; i++) WireCell(_cells[i], i);
        Loaded += (_, _) => { try { MaskedEmailTxt.Text = Mask(ConfigStore.Load().GmailAddress); } catch { MaskedEmailTxt.Text = "your Gmail"; } };
    }

    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { try { DialogResult = false; } catch { } Close(); }

    private void WireCell(TextBox tb, int i)
    {
        tb.PreviewTextInput += (s, e) =>
        {
            if (!char.IsDigit(e.Text, 0)) { e.Handled = true; return; }
        };
        tb.TextChanged += (s, e) =>
        {
            if (tb.Text.Length > 0 && i < _cells.Length - 1) _cells[i + 1].Focus();
        };
        tb.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Back && tb.Text.Length == 0 && i > 0) { _cells[i - 1].Focus(); _cells[i - 1].SelectAll(); }
        };
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        ErrorTxt.Text = "";
        SendBtn.IsEnabled = false;
        SendBtn.Content = "Sending…";
        try
        {
            var cfg = ConfigStore.Load();
            _otp = MailService.GenerateOtp();
            _otpExpiry = DateTime.UtcNow.AddMinutes(10);
            await MailService.SendOtpAsync(cfg.GmailAddress, cfg.GmailAppPassword, _otp);
            GoToStep2();
        }
        catch (Exception ex)
        {
            ErrorTxt.Text = "Failed to send: " + ex.Message;
            SendBtn.Content = "Send code to Gmail";
            SendBtn.IsEnabled = true;
        }
    }

    private async void Resend_Click(object sender, RoutedEventArgs e)
    {
        ErrorTxt.Text = "";
        try
        {
            var cfg = ConfigStore.Load();
            _otp = MailService.GenerateOtp();
            _otpExpiry = DateTime.UtcNow.AddMinutes(10);
            await MailService.SendOtpAsync(cfg.GmailAddress, cfg.GmailAppPassword, _otp);
            ErrorTxt.Foreground = (System.Windows.Media.Brush)FindResource("Accent");
            ErrorTxt.Text = "New code sent.";
        }
        catch (Exception ex) { ErrorTxt.Text = "Resend failed: " + ex.Message; }
    }

    private void GoToStep2()
    {
        Step1Panel.Visibility = Visibility.Collapsed;
        Step2Panel.Visibility = Visibility.Visible;
        // Step indicator: 1 = done, 2 = on
        Step1Num.Background = (System.Windows.Media.Brush)FindResource("Safe");
        Step1Num.BorderBrush = (System.Windows.Media.Brush)FindResource("Safe");
        Step1Txt.Text = "✓";
        Step1Txt.Foreground = (System.Windows.Media.Brush)FindResource("SafeInk");
        Step2Num.Background = (System.Windows.Media.Brush)FindResource("Accent");
        Step2Num.BorderBrush = (System.Windows.Media.Brush)FindResource("Accent");
        ((TextBlock)Step2Num.Child).Foreground = (System.Windows.Media.Brush)FindResource("OnAccent");
        Step2Label.Foreground = (System.Windows.Media.Brush)FindResource("FgHeading");
        Otp0.Focus();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        Step1Panel.Visibility = Visibility.Visible;
        Step2Panel.Visibility = Visibility.Collapsed;
        Step1Num.Background = (System.Windows.Media.Brush)FindResource("Accent");
        Step1Num.BorderBrush = (System.Windows.Media.Brush)FindResource("Accent");
        Step1Txt.Text = "1";
        Step1Txt.Foreground = (System.Windows.Media.Brush)FindResource("OnAccent");
        Step2Num.Background = System.Windows.Media.Brushes.Transparent;
        Step2Num.BorderBrush = (System.Windows.Media.Brush)FindResource("BorderStrong");
        ((TextBlock)Step2Num.Child).Foreground = (System.Windows.Media.Brush)FindResource("FgFaint");
        Step2Label.Foreground = (System.Windows.Media.Brush)FindResource("FgFaint");
        SendBtn.Content = "Resend code to Gmail";
        SendBtn.IsEnabled = true;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ErrorTxt.Foreground = (System.Windows.Media.Brush)FindResource("Danger");
        ErrorTxt.Text = "";
        var entered = string.Concat(_cells.Select(c => c.Text));
        if (_otp == null || DateTime.UtcNow > _otpExpiry) { ErrorTxt.Text = "Code expired. Resend a new one."; return; }
        if (entered.Length < 6) { ErrorTxt.Text = "Enter all 6 digits."; return; }
        if (entered != _otp)    { ErrorTxt.Text = "Wrong code."; return; }
        if (NewPwd.Password.Length < 6) { ErrorTxt.Text = "New password must be at least 6 characters."; return; }

        var cfg = ConfigStore.Load();
        var (salt, hash) = ConfigStore.HashPassword(NewPwd.Password);
        cfg.Salt = salt;
        cfg.PasswordHash = hash;
        ConfigStore.Save(cfg);
        try { DialogResult = true; } catch { }
        Close();
    }

    private static string Mask(string email)
    {
        if (string.IsNullOrEmpty(email)) return "your Gmail";
        var at = email.IndexOf('@');
        if (at <= 2) return email;
        return email[..1] + new string('•', Math.Max(1, at - 2)) + email[(at - 1)..];
    }
}
