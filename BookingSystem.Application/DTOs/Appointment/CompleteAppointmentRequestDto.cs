using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Appointment;

public class CompleteAppointmentRequestDto
{
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
