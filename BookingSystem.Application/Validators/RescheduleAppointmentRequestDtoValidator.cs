using BookingSystem.Application.DTOs.Appointment;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class RescheduleAppointmentRequestDtoValidator : AbstractValidator<RescheduleAppointmentRequestDto>
{
    public RescheduleAppointmentRequestDtoValidator()
    {
        RuleFor(x => x.NewTimeSlotId)
            .GreaterThan(0)
            .WithMessage("New time slot ID must be greater than 0");
    }
}
