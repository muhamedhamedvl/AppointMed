using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Auth;

public class ResendVerificationRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
