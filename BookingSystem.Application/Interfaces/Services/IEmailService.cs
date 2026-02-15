namespace BookingSystem.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendVerificationEmailAsync(string toEmail, string verificationCode, string userName);
    Task SendPasswordResetEmailAsync(string toEmail, string token);
    Task SendOtpEmailAsync(string toEmail, string otpCode);
}
