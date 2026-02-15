using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Clinic;

public class UpdateClinicRequestDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    public TimeSpan? OpeningTime { get; set; }

    public TimeSpan? ClosingTime { get; set; }
}
