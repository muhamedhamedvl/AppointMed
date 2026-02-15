using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Doctor;

public class UpdateDoctorRequestDto
{
    [MaxLength(100)]
    public string? Specialization { get; set; }

    [Range(0, 70)]
    public int? YearsOfExperience { get; set; }

    [Range(0, 100000)]
    public decimal? ConsultationFee { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public int? ClinicId { get; set; }
    
    public bool? IsAvailable { get; set; }
}
