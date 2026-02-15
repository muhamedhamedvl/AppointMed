using BookingSystem.Application.DTOs.Clinic;

namespace BookingSystem.Application.DTOs.Doctor;

public class DoctorDetailDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? Bio { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsApproved { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public ClinicDto? Clinic { get; set; }
    public DateTime CreatedAt { get; set; }
}
