using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Doctor;

public class OnboardDoctorRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Range(0, 70)]
    public int YearsOfExperience { get; set; }

    [Range(0, 100000)]
    public decimal ConsultationFee { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    [Required]
    public int ClinicId { get; set; }
}
