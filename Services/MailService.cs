using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace BrowserGate.Services;

public static class MailService
{
    public static string GenerateOtp()
    {
        var n = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return n.ToString("D6");
    }

    public static async Task SendOtpAsync(string gmail, string appPassword, string otp)
    {
        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(gmail, appPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        using var msg = new MailMessage(gmail, gmail)
        {
            Subject = "BrowserGate â€” Password Reset Code",
            Body = $"Your BrowserGate reset code is: {otp}\n\nValid for 10 minutes. If you did not request this, ignore.",
            IsBodyHtml = false
        };
        await smtp.SendMailAsync(msg);
    }
}
