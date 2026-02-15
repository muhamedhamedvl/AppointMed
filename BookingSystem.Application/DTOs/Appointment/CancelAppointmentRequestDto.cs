using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Appointment;

public class CancelAppointmentRequestDto
{
    [Required]
    [MaxLength(500)]
    public string CancellationReason { get; set; } = string.Empty;
}
