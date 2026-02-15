using BookingSystem.Application.DTOs.Appointment;
using MediatR;

namespace BookingSystem.Application.Features.Appointments.Commands;

public class CancelAppointmentCommand : IRequest<AppointmentDto>
{
    public int AppointmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public CancelAppointmentRequestDto Request { get; set; } = null!;
}
