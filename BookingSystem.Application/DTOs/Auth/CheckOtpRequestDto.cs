using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Auth;

public class CheckOtpRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}
