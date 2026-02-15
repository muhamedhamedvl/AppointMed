namespace BookingSystem.Application.Interfaces.Services;

public interface IEmailQueueService
{
    Task QueueEmailAsync(string toEmail, string subject, string htmlBody);
    Task QueueVerificationEmailAsync(string toEmail, string verificationCode, string userName);
    Task QueuePasswordResetEmailAsync(string toEmail, string resetToken);
    Task QueueOtpEmailAsync(string toEmail, string otpCode);
}
