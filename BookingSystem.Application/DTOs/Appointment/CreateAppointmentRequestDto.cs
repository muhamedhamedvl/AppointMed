using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Appointment;

public class CreateAppointmentRequestDto
{
    [Required]
    public int DoctorId { get; set; }

    [Required]
    public int TimeSlotId { get; set; }

    [MaxLength(500)]
    public string? ReasonForVisit { get; set; }
}
