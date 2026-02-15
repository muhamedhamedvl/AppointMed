using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.User;

public class UpdateUserDto
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? Role { get; set; } // Admin can change roles
}
