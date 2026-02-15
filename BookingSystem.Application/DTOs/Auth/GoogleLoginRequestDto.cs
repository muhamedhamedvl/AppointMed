using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Auth;

public class GoogleLoginRequestDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
