using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Infrastructure.BackgroundServices;

public class EmailProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailProcessorWorker> _logger;

    public EmailProcessorWorker(IServiceProvider serviceProvider, ILogger<EmailProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Processor Worker started at: {Time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing email queue");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("Email Processor Worker stopped at: {Time}", DateTime.UtcNow);
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailQueueRepository = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var pendingEmails = (await emailQueueRepository.GetPendingAsync(10)).ToList();

        if (pendingEmails.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending emails", pendingEmails.Count);

        foreach (var email in pendingEmails)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await emailService.SendEmailAsync(email.ToEmail, email.Subject, email.HtmlBody);

                email.Status = EmailStatus.Sent;
                email.SentAt = DateTime.UtcNow;
                email.LastAttemptAt = DateTime.UtcNow;
                email.ErrorMessage = null;

                _logger.LogInformation(
                    "Email sent successfully. ID: {EmailId}, To: {ToEmail}, Subject: {Subject}",
                    email.Id,
                    email.ToEmail,
                    email.Subject
                );
            }
            catch (Exception ex)
            {
                email.RetryCount++;
                email.LastAttemptAt = DateTime.UtcNow;
                email.ErrorMessage = ex.Message;

                if (email.RetryCount >= email.MaxRetries)
                {
                    email.Status = EmailStatus.Failed;

                    _logger.LogError(
                        ex,
                        "Email failed after {RetryCount} attempts. ID: {EmailId}, To: {ToEmail}, Subject: {Subject}",
                        email.RetryCount,
                        email.Id,
                        email.ToEmail,
                        email.Subject
                    );
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Email send failed (attempt {RetryCount}/{MaxRetries}). ID: {EmailId}, To: {ToEmail}",
                        email.RetryCount,
                        email.MaxRetries,
                        email.Id,
                        email.ToEmail
                    );
                }
            }

            await emailQueueRepository.UpdateAsync(email);
        }

        await emailQueueRepository.SaveChangesAsync();
    }
}
