using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Appointment;

public class RescheduleAppointmentRequestDto
{
    [Required]
    public int NewTimeSlotId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}
