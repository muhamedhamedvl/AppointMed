using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Entities;

public class EmailQueue
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string HtmlBody { get; set; } = null!;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}
