using BookingSystem.Application.DTOs.Appointment;
using BookingSystem.Application.Features.Appointments.Commands;
using BookingSystem.Application.Interfaces.Services;
using MediatR;

namespace BookingSystem.Application.Features.Appointments.Handlers;

public class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, AppointmentDto>
{
    private readonly IAppointmentService _appointmentService;

    public BookAppointmentCommandHandler(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    public async Task<AppointmentDto> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        return await _appointmentService.BookAppointmentAsync(request.UserId, request.Request);
    }
}
