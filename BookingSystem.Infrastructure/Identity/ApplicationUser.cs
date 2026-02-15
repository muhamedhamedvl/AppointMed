using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Email verification code (6-digit OTP)
    public string? VerificationCode { get; set; }
    public DateTime? VerificationCodeExpiry { get; set; }
    
    // OTP for additional verification (can be used for 2FA or password reset)
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }
    
    // Refresh token for JWT token refresh flow
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime? RefreshTokenCreatedAt { get; set; }
}
