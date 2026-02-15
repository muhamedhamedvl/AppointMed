using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.User;

public class UpdateMyProfileDto
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
}
