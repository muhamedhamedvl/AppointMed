using BookingSystem.Application.DTOs.Appointment;
using MediatR;

namespace BookingSystem.Application.Features.Appointments.Commands;

public class BookAppointmentCommand : IRequest<AppointmentDto>
{
    public string UserId { get; set; } = string.Empty;
    public CreateAppointmentRequestDto Request { get; set; } = null!;
}
