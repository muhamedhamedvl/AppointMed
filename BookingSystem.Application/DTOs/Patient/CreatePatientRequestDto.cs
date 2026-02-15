using BookingSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Patient;

public class CreatePatientRequestDto
{
    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [MaxLength(10)]
    public string? BloodGroup { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? EmergencyContact { get; set; }

    [MaxLength(2000)]
    public string? MedicalHistory { get; set; }
}
