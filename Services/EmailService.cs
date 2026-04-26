using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Configuration;

namespace LegalCaseAPI.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpHost = _config["Smtp:Host"];
        var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
        var smtpUser = _config["Smtp:Username"];
        var smtpPass = _config["Smtp:Password"];
        var senderName = _config["Smtp:SenderName"];
        var senderEmail = _config["Smtp:SenderEmail"];

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(smtpUser, smtpPass);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendTwoFactorCodeAsync(string to, string code)
    {
        var body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 10px; max-width: 500px;'>
                <h2 style='color: #2c3e50;'>Your Security Code</h2>
                <p>Hello,</p>
                <p>Use the following code to complete your sign-in to Legal Case Management:</p>
                <div style='font-size: 24px; font-weight: bold; background: #f4f4f4; padding: 15px; text-align: center; letter-spacing: 5px; color: #e74c3c;'>
                    {code}
                </div>
                <p style='color: #7f8c8d; font-size: 12px; margin-top: 20px;'>
                    If you didn't request this code, please ignore this email or change your password.
                    The code is valid for 10 minutes.
                </p>
            </div>";
        
        await SendEmailAsync(to, "Verification Code - Legal Case Management", body);
    }

    public async Task SendPasswordResetAsync(string to, string token)
    {
        // In a real app, this would be a link to your frontend
        var resetLink = $"http://localhost:5173/reset-password?token={token}";
        var body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 10px; max-width: 500px;'>
                <h2 style='color: #2c3e50;'>Reset Your Password</h2>
                <p>Hello,</p>
                <p>We received a request to reset your password. Click the button below to proceed:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background: #3498db; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
                </div>
                <p>Or copy and paste this link into your browser:</p>
                <p style='word-break: break-all; color: #3498db;'>{resetLink}</p>
                <p style='color: #7f8c8d; font-size: 12px; margin-top: 20px;'>
                    If you didn't request a password reset, you can safely ignore this email.
                    The link will expire in 1 hour.
                </p>
            </div>";

        await SendEmailAsync(to, "Password Reset - Legal Case Management", body);
    }
}
