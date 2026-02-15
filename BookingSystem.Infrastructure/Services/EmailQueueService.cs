using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Infrastructure.Services;

public class EmailQueueService : IEmailQueueService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailQueueService> _logger;

    public EmailQueueService(ApplicationDbContext context, ILogger<EmailQueueService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task QueueEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var emailQueue = new EmailQueue
        {
            ToEmail = toEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            Status = EmailStatus.Pending,
            RetryCount = 0,
            MaxRetries = 3,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailQueues.Add(emailQueue);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Email queued successfully. ID: {EmailId}, To: {ToEmail}, Subject: {Subject}",
            emailQueue.Id,
            toEmail,
            subject
        );
    }

    public async Task QueueVerificationEmailAsync(string toEmail, string verificationCode, string userName)
    {
        var subject = "Verify Your AppointMed Account";
        var htmlBody = BuildVerificationEmail(verificationCode, userName);
        await QueueEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task QueuePasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Reset Your AppointMed Password";
        var htmlBody = BuildPasswordResetEmail(resetToken);
        await QueueEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task QueueOtpEmailAsync(string toEmail, string otpCode)
    {
        var subject = "Your AppointMed OTP Code";
        var htmlBody = BuildOtpEmail(otpCode);
        await QueueEmailAsync(toEmail, subject, htmlBody);
    }

    private string BuildVerificationEmail(string verificationCode, string userName)
    {
        return BuildEmail(
            title: "Verify your account",
            greeting: $"Hello <strong>{userName}</strong>,",
            mainMessage: "Thank you for creating an account with AppointMed. To complete your registration, please verify your email address using the code below:",
            verificationCode: verificationCode,
            ctaButtonText: "Verify Account",
            ctaButtonLink: "#",
            warningMessage: "This code expires in 24 hours. If you didn't create this account, please ignore this email or contact support immediately.",
            footerNote: "Need help? Our support team is available 24/7."
        );
    }

    private string BuildPasswordResetEmail(string resetToken)
    {
        return BuildEmail(
            title: "Reset your password",
            greeting: "Hello,",
            mainMessage: "We received a request to reset your password. Use the button below to create a new password:",
            ctaButtonText: "Reset Password",
            ctaButtonLink: $"#/reset-password?token={resetToken}",
            warningMessage: "This link expires in 1 hour. If you didn't request a password reset, please ignore this email or contact support if you have concerns.",
            footerNote: "For security reasons, never share this link with anyone."
        );
    }

    private string BuildOtpEmail(string otpCode)
    {
        return BuildEmail(
            title: "Your verification code",
            greeting: "Hello,",
            mainMessage: "Your one-time password (OTP) code is:",
            verificationCode: otpCode,
            warningMessage: "This code expires in 5 minutes. Do not share this code with anyone.",
            footerNote: "If you didn't request this code, please contact support immediately."
        );
    }

    private string BuildEmail(
        string title,
        string? greeting = null,
        string? mainMessage = null,
        string? verificationCode = null,
        string? ctaButtonText = null,
        string? ctaButtonLink = null,
        string? warningMessage = null,
        string? footerNote = null)
    {
        var greetingHtml = !string.IsNullOrEmpty(greeting)
            ? $@"<p style=""margin: 0 0 8px 0; font-size: 15px; color: #374151; line-height: 1.6;"">{greeting}</p>"
            : "";

        var mainMessageHtml = !string.IsNullOrEmpty(mainMessage)
            ? $@"<p style=""margin: 0 0 32px 0; font-size: 15px; color: #374151; line-height: 1.6;"">{mainMessage}</p>"
            : "";

        var verificationCodeHtml = !string.IsNullOrEmpty(verificationCode)
            ? $@"<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""margin: 0 0 32px 0;"">
                    <tr>
                        <td align=""center"" style=""padding: 32px 20px; background-color: #f9fafb; border: 1px solid #e5e7eb; border-radius: 6px;"">
                            <div style=""font-size: 36px; font-weight: 700; color: #111827; letter-spacing: 8px; font-family: 'Courier New', Courier, monospace;"">{verificationCode}</div>
                        </td>
                    </tr>
                </table>"
            : "";


        var warningHtml = !string.IsNullOrEmpty(warningMessage)
            ? $@"<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""margin: 0 0 24px 0;"">
                    <tr>
                        <td style=""padding: 16px; background-color: #fef3c7; border-left: 3px solid #f59e0b; border-radius: 4px;"">
                            <p style=""margin: 0; font-size: 13px; color: #92400e; line-height: 1.5;""><strong style=""font-weight: 600;"">Security Notice:</strong> {warningMessage}</p>
                        </td>
                    </tr>
                </table>"
            : "";

        var footerNoteHtml = !string.IsNullOrEmpty(footerNote)
            ? $@"<p style=""margin: 0; font-size: 13px; color: #6b7280; line-height: 1.5;"">{footerNote}</p>"
            : "";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <title>{title} - AppointMed</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Helvetica Neue', Arial, sans-serif; background-color: #f5f7fb; line-height: 1.6;"">
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #f5f7fb; padding: 40px 0;"">
        <tr>
            <td align=""center"" style=""padding: 0 20px;"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""600"" style=""max-width: 600px; background-color: #ffffff; border-radius: 8px; border: 1px solid #e5e7eb; box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""padding: 48px 40px 32px 40px; text-align: center;"">
                            <h1 style=""margin: 0 0 8px 0; font-size: 24px; font-weight: 600; color: #111827; letter-spacing: -0.5px;"">AppointMed</h1>
                            <p style=""margin: 0; font-size: 14px; color: #6b7280; font-weight: 400;"">Your Trusted Healthcare Partner</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 0 40px 40px 40px;"">
                            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: #111827; line-height: 1.3;"">{title}</h2>
                            {greetingHtml}
                            {mainMessageHtml}
                            {verificationCodeHtml}
                            {warningHtml}
                            {footerNoteHtml}
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px 40px; background-color: #f9fafb; border-top: 1px solid #e5e7eb; border-radius: 0 0 8px 8px;"">
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                                <tr>
                                    <td align=""center"" style=""padding: 0 0 8px 0;"">
                                        <p style=""margin: 0; font-size: 12px; color: #9ca3af; line-height: 1.5;"">© 2026 AppointMed. All rights reserved.</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"">
                                        <p style=""margin: 0; font-size: 11px; color: #9ca3af; line-height: 1.4;"">This is an automated message, please do not reply.<br>Contact us at <a href=""mailto:support@appointmed.com"" style=""color: #6b7280; text-decoration: none;"">support@appointmed.com</a></p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
