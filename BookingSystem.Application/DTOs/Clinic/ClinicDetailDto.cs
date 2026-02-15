using BookingSystem.Application.DTOs.Doctor;

namespace BookingSystem.Application.DTOs.Clinic;

public class ClinicDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }
    public int DoctorCount { get; set; }
    public List<DoctorProfileDto> Doctors { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
