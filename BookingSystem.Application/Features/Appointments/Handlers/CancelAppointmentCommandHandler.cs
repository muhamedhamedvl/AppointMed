using BookingSystem.Application.DTOs.Appointment;
using BookingSystem.Application.Features.Appointments.Commands;
using BookingSystem.Application.Interfaces.Services;
using MediatR;

namespace BookingSystem.Application.Features.Appointments.Handlers;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, AppointmentDto>
{
    private readonly IAppointmentService _appointmentService;

    public CancelAppointmentCommandHandler(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    public async Task<AppointmentDto> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        return await _appointmentService.CancelAppointmentAsync(
            request.AppointmentId, request.UserId, request.Request);
    }
}
