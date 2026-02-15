namespace BookingSystem.Application.DTOs.User;

public class UserResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
}
