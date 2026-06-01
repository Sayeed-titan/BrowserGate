using System.Windows;
using System.Windows.Input;
using EdgeLocker.Services;

namespace EdgeLocker.Views;

public partial class ResetWindow : Window
{
    private string? _otp;
    private DateTime _otpExpiry;

    public ResetWindow() { InitializeComponent(); }

    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { try { DialogResult = false; } catch { } Close(); }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        ErrorTxt.Text = "";
        SendBtn.IsEnabled = false;
        SendBtn.Content = "Sending...";
        try
        {
            var cfg = ConfigStore.Load();
            _otp = MailService.GenerateOtp();
            _otpExpiry = DateTime.UtcNow.AddMinutes(10);
            await MailService.SendOtpAsync(cfg.GmailAddress, cfg.GmailAppPassword, _otp);
            StepTxt.Text = $"Code sent to {Mask(cfg.GmailAddress)}. Enter it below with your new password.";
            OtpBox.IsEnabled = NewPwd.IsEnabled = NewPwd2.IsEnabled = ConfirmBtn.IsEnabled = true;
            SendBtn.Content = "Resend code";
            SendBtn.IsEnabled = true;
            OtpBox.Focus();
        }
        catch (Exception ex)
        {
            ErrorTxt.Text = "Failed to send: " + ex.Message;
            SendBtn.Content = "Send code to my Gmail";
            SendBtn.IsEnabled = true;
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ErrorTxt.Text = "";
        if (_otp == null || DateTime.UtcNow > _otpExpiry) { ErrorTxt.Text = "Code expired. Resend."; return; }
        if (OtpBox.Text.Trim() != _otp)       { ErrorTxt.Text = "Wrong code."; return; }
        if (NewPwd.Password.Length < 6)       { ErrorTxt.Text = "Password too short."; return; }
        if (NewPwd.Password != NewPwd2.Password) { ErrorTxt.Text = "Passwords don't match."; return; }

        var cfg = ConfigStore.Load();
        var (salt, hash) = ConfigStore.HashPassword(NewPwd.Password);
        cfg.Salt = salt;
        cfg.PasswordHash = hash;
        ConfigStore.Save(cfg);
        DialogResult = true;
        Close();
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 2) return email;
        return email[..2] + new string('*', at - 2) + email[at..];
    }
}
